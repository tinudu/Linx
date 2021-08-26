using Linx.Queueing;
using Linx.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        private static readonly InvalidOperationException _queueVersionConflict = new("Queue version conflict.");

        /// <summary>
        /// Decouples the source from its consumer by using a queue.
        /// </summary>
        /// <param name="source">The source sequence.</param>
        /// <param name="queueFactory">Function to create a new <see cref="IQueue{TIn, TOut}"/>.</param>
        /// <param name="throwOnQueueFull">Specifies whether the resulting sequence terminates with an exception in case the queue gets full.</param>
        public static IAsyncEnumerable<QueueReader<R>> Queue<S, R>(
            this IAsyncEnumerable<S> source,
            Func<IQueue<S, R>> queueFactory,
            bool throwOnQueueFull)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (queueFactory is null) throw new ArgumentNullException(nameof(queueFactory));

            return Create(token => new QueueEnumerator<S, R>(source, queueFactory, throwOnQueueFull, token));
        }

        private sealed class QueueEnumerator<S, R> : IAsyncEnumerator<QueueReader<R>>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCompleted = 3; // source completed with or without error, but there are still items in the queue
            private const int _sError = 4; // canceled or disposed while still producing items
            private const int _sFinal = 5;

            private static readonly Func<IQueue<S, R>, R> _dequeueOne = q => q.Dequeue();
            private static readonly Func<IQueue<S, R>, IReadOnlyList<R>> _dequeueAll = q => q.DequeueAll();

            private readonly IAsyncEnumerable<S> _source;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private readonly CancellationTokenSource _cts = new();
            private readonly IQueue<S, R> _queue;
            private readonly IProvider _provider;
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed; // set when completed or final
            private short _version;
            private int _state;
            private Exception _error;

            public QueueEnumerator(IAsyncEnumerable<S> source, Func<IQueue<S, R>> queueFactory, bool throwOnQueueFull, CancellationToken token)
            {
                _source = source;
                _queue = queueFactory();
                _provider = throwOnQueueFull ? new ThrowingProvider(this) : new AwaitingProvider(this);
                Current = new(_provider, default);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetError(new OperationCanceledException(token)));
            }

            public QueueReader<R> Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsAccepting.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        if (_queue.IsEmpty)
                        {
                            _state = _sAccepting;
                            _provider.Produce();
                        }
                        else // queue contains initial items
                        {
                            Current = new(_provider, unchecked(++_version));
                            _state = _sInitial;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sEmitting:
                        if (_queue.IsEmpty)
                            _state = _sAccepting;
                        else
                        {
                            Current = new(_provider, unchecked(++_version));
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sCompleted:
                        if (_queue.IsEmpty)
                        {
                            _state = _sFinal;
                            _ctr.Dispose();
                            _tsAccepting.SetExceptionOrResult(_error, false);
                        }
                        else
                        {
                            Current = new(_provider, unchecked(++_version));
                            _state = _sCompleted;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sError:
                        _state = _sError;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // accepting???
                        _state = state;
                        break;
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                SetError(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void SetError(Exception error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _tsAccepting.SetException(error);
                        _cts.Cancel();
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _cts.Cancel();
                        break;

                    case _sCompleted:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        break;

                    default: // already error, or final
                        _state = state;
                        break;
                }
            }

            private void SetCompleted(Exception errorOrNot)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        // queue must be empty, unless its implementation is buggy
                        _error = errorOrNot;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsAccepting.SetExceptionOrResult(errorOrNot, false);
                        _cts.Cancel();
                        break;

                    case _sEmitting:
                        _error = errorOrNot;
                        _state = _sCompleted;
                        _atmbDisposed.SetResult();
                        _cts.Cancel();
                        break;

                    case _sError:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default: // ???
                        _state = state;
                        _atmbDisposed.SetResult();
                        break;
                }
            }

            private interface IProvider : QueueReader<R>.IProvider
            {
                void Produce();
            }

            private sealed class AwaitingProvider : IProvider
            {
                private readonly QueueEnumerator<S, R> _e;
                private ManualResetValueTaskSource _tsEnqueuePending;

                public AwaitingProvider(QueueEnumerator<S, R> e)
                {
                    _e = e;
                }

                public R Dequeue(short version) => Dequeue(_dequeueOne, version);
                public IReadOnlyList<R> DequeueAll(short version) => Dequeue(_dequeueAll, version);

                private T Dequeue<T>(Func<IQueue<S, R>, T> dequeue, short version)
                {
                    var state = Atomic.Lock(ref _e._state);
                    try
                    {
                        if (version != _e._version) throw _queueVersionConflict;
                        return dequeue(_e._queue);
                    }
                    finally
                    {
                        var ts = Linx.Clear(ref _tsEnqueuePending);
                        _e._state = state;
                        ts?.SetResult();
                    }
                }

                public async void Produce()
                {
                    Exception error = null;
                    try
                    {
                        ManualResetValueTaskSource ts = new();
                        using var ctr = _e._cts.Token.Register(() =>
                        {
                            var state = Atomic.Lock(ref _e._state);
                            var ts = Linx.Clear(ref _tsEnqueuePending);
                            _e._state = state;
                            ts?.SetResult();
                        });

                        await foreach (var item in _e._source.WithCancellation(_e._cts.Token).ConfigureAwait(false))
                            while (true)
                            {
                                var state = Atomic.Lock(ref _e._state);
                                switch (state)
                                {
                                    case _sAccepting:
                                    case _sEmitting:
                                        break;

                                    default:
                                        _e._state = state;
                                        return;
                                }

                                if (_e._queue.IsFull)
                                {
                                    ts.Reset();
                                    _tsEnqueuePending = ts;
                                    _e._state = state;
                                    await ts.Task.ConfigureAwait(false);
                                    continue;
                                }

                                try { _e._queue.Enqueue(item); }
                                catch (Exception) { _e._state = state; throw; }

                                bool completeMoveNext;
                                if (state == _sAccepting && !_e._queue.IsEmpty)
                                {
                                    completeMoveNext = true;
                                    _e.Current = new(this, unchecked(++_e._version));
                                    _e._state = _sEmitting;
                                }
                                else
                                {
                                    completeMoveNext = false;
                                    _e._state = state;
                                }

                                if (completeMoveNext)
                                    _e._tsAccepting.SetResult(true);

                                break;
                            }
                    }
                    catch (Exception ex) { error = ex; }
                    finally { _e.SetCompleted(error); }
                }
            }

            private sealed class ThrowingProvider : IProvider
            {
                private readonly QueueEnumerator<S, R> _e;

                public ThrowingProvider(QueueEnumerator<S, R> e)
                {
                    _e = e;
                }

                public R Dequeue(short version) => Dequeue(_dequeueOne, version);
                public IReadOnlyList<R> DequeueAll(short version) => Dequeue(_dequeueAll, version);

                private T Dequeue<T>(Func<IQueue<S, R>, T> dequeue, short version)
                {
                    var state = Atomic.Lock(ref _e._state);
                    try
                    {
                        if (version != _e._version) throw _queueVersionConflict;
                        return dequeue(_e._queue);
                    }
                    finally
                    {
                        _e._state = state;
                    }
                }


                private bool Enqueue(S item)
                {
                    var state = Atomic.Lock(ref _e._state);
                    switch (state)
                    {
                        case _sAccepting:
                        case _sEmitting:
                            Exception error = null;
                            try { _e._queue.Enqueue(item); }
                            catch (Exception ex) { error = ex; }

                            bool completeMoveNext;
                            if (state == _sAccepting && !_e._queue.IsEmpty)
                            {
                                completeMoveNext = true;
                                _e.Current = new(this, unchecked(++_e._version));
                                _e._state = _sEmitting;
                            }
                            else
                            {
                                completeMoveNext = false;
                                _e._state = state;
                            }

                            if (completeMoveNext)
                                _e._tsAccepting.SetResult(true);
                            return error is null ? true : throw error;

                        default: // probably _sError
                            return false;
                    }
                }

                public async void Produce()
                {
                    Exception error = null;
                    try
                    {
                        await foreach (var item in _e._source.WithCancellation(_e._cts.Token).ConfigureAwait(false))
                            if (!Enqueue(item))
                                break;
                    }
                    catch (Exception ex) { error = ex; }

                    _e.SetCompleted(error);
                }
            }
        }
    }
}
