namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Queueing;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Queueing <see cref="IAsyncEnumerable{T}"/> to <see cref="IAsyncEnumerable{T}"/> enumerator.
        /// </summary>
        private sealed class QueueingEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sError = 4; // but not completed
            private const int _sCompleted = 5; // but not error
            private const int _sFinal = 6;

            private readonly IAsyncEnumerable<T> _source;
            private readonly IQueue<T> _queue;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private ManualResetValueTaskSource<bool> _tsQueueFull;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbDisposed = default;

            public QueueingEnumerator(IAsyncEnumerable<T> source, IQueue<T> queue, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(queue != null);

                _source = source;
                _queue = queue;
                _token = token;
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsAccepting.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sAccepting;
                        Produce();
                        break;

                    case _sEmitting:
                        var tsQueueFull = Linx.Clear(ref _tsQueueFull);
                        if (_queue.IsEmpty)
                            _state = _sAccepting;
                        else
                        {
                            Current = _queue.Dequeue();
                            if (_queue.IsEmpty) // consumer now faster than producer
                                try { _queue.TrimExcess(); }
                                catch {/**/}
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }

                        tsQueueFull?.SetResult(true);
                        break;

                    case _sError:
                        _state = _sError;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sCompleted:
                        if (_queue.IsEmpty)
                        {
                            Current = default;
                            _state = _sCompleted;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                        }
                        else
                        {
                            Current = _queue.Dequeue();
                            _state = _sCompleted;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting???
                        _state = state;
                        throw new Exception(state + "???");
                }
                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                OnError(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void OnError(Exception error)
            {
                Debug.Assert(error != null);

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
                        Current = default;
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Debug.Assert(_queue.IsEmpty);
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        var tsQueueFull = Linx.Clear(ref _tsQueueFull);
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _queue.Clear();
                        tsQueueFull?.SetResult(false);
                        break;

                    case _sCompleted:
                        if (_queue.IsEmpty)
                        {
                            _error = error;
                            _queue.Clear();
                        }
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    default: // Completed, Final
                        _state = state;
                        break;
                }
            }

            private async void Produce()
            {
                try
                {
                    var tsQueueFull = new ManualResetValueTaskSource<bool>();
                    await foreach (var item in _source.WithCancellation(_token).ConfigureAwait(false))
                        while (true)
                        {
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting:
                                    Current = item;
                                    _state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                    break;

                                case _sEmitting:
                                    if (_queue.IsFull)
                                    {
                                        tsQueueFull.Reset();
                                        _tsQueueFull = tsQueueFull;
                                        _state = _sEmitting;
                                        if (!await tsQueueFull.Task.ConfigureAwait(false))
                                            return;
                                        continue;
                                    }
                                    try
                                    {
                                        _queue.Enqueue(item);
                                        _state = _sEmitting;
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        _state = _sEmitting;
                                        OnError(ex);
                                        return;
                                    }

                                default:
                                    _state = state;
                                    return;
                            }

                            break;
                        }
                }
                catch (Exception ex) { OnError(ex); }
                finally
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert(_queue.IsEmpty && _error == null);
                            Current = default;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetResult(false);
                            break;

                        case _sEmitting:
                            if (_queue.IsEmpty)
                            {
                                _state = _sFinal;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        case _sError:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        default:
                            _state = state;
                            break;
                    }

                }
            }
        }
    }
}