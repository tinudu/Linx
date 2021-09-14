namespace Linx.AsyncEnumerable
{
    using global::Linx.Collections;
    using global::Linx.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Decouples the source from its consumer; buffered items are retrieved individually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source) => source.Buffer(int.MaxValue, false);

        /// <summary>
        /// Decouples the source from its consumer; buffered items are retrieved individually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is non-positive.</exception>
        /// <remarks>
        /// <paramref name="backpressure"/> controls what happens if <paramref name="maxCapacity"/> is reached:
        /// A value of true excerts backpressure on the source.
        /// A value of false terminates the sequence with an exception.
        /// </remarks>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxCapacity, bool backpressure)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            return new BufferAsyncEnumerable<T>(source, maxCapacity, backpressure);
        }

        private sealed class BufferAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> _source;
            private readonly int _maxCapacity;
            private readonly bool _backpressure;

            public BufferAsyncEnumerable(IAsyncEnumerable<T> source, int maxCapacity, bool backpressure)
            {
                Debug.Assert(source is not null);
                Debug.Assert(maxCapacity > 0);

                _source = source;
                _maxCapacity = maxCapacity;
                _backpressure = backpressure;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) =>
                token.IsCancellationRequested ?
                    new ThrowIterator<T>(new OperationCanceledException(token)) :
                    new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sEmitting = 0;
                private const int _sAccepting = 1;
                private const int _sCompleted = 2;
                private const int _sFinal = 3;

                private readonly BufferAsyncEnumerable<T> _e;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
                private ManualResetValueTaskSource<bool> _tsEmitting;
                private readonly CancellationTokenRegistration _ctr;
                private readonly AsyncTaskMethodBuilder _atmbDisposed;
                private int _state;
                private Exception _error;
                private LinxQueue<T> _queue;

                public Enumerator(BufferAsyncEnumerable<T> e, CancellationToken token)
                {
                    _e = e;
                    _queue = new(e._maxCapacity, false);
                    Produce(token);
                    if (token.CanBeCanceled)
                        _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sEmitting:
                            var tsEmitting = Linx.Clear(ref _tsEmitting);
                            if (_queue.Count == 0)
                                _state = _sAccepting;
                            else // dequeue
                            {
                                Current = _queue.DequeueOne();
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            tsEmitting?.SetResult(true);
                            break;

                        case _sCompleted:
                            Debug.Assert(_queue.Count > 0);
                            Current = _queue.DequeueOne();
                            if (_queue.Count > 0)
                                _state = _sCompleted;
                            else
                            {
                                _state = _sFinal;
                                _ctr.Dispose();
                            }
                            _tsAccepting.SetResult(true);
                            break;

                        case _sFinal:
                            Current = default;
                            _state = _sFinal;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // _sAccepting???
                            _state = _sAccepting;
                            SetFinal(new Exception(state + "???"));
                            break;
                    }

                    return _tsAccepting.Task;
                }

                public async ValueTask DisposeAsync()
                {
                    SetFinal(AsyncEnumeratorDisposedException.Instance);
                    Current = default;
                    await _atmbDisposed.Task.ConfigureAwait(false);
                }

                private void SetFinal(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sEmitting:
                            var tsBp = Linx.Clear(ref _tsEmitting);
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            tsBp?.SetResult(false);
                            break;

                        case _sAccepting:
                            Debug.Assert(_queue.Count == 0);
                            Current = default;
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _tsAccepting.SetExceptionOrResult(error, false);
                            break;

                        case _sCompleted:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            break;

                        default: // _sFinal
                            _state = state;
                            break;
                    }
                }

                private async void Produce(CancellationToken token)
                {
                    Exception error = null;
                    try
                    {
                        ManualResetValueTaskSource<bool> tsEmitting = new();
                        tsEmitting.Reset();
                        _tsEmitting = tsEmitting;

                        if (!await tsEmitting.Task.ConfigureAwait(false))
                            return;
                        tsEmitting.Reset();

                        await foreach (var item in _e._source.WithCancellation(token).ConfigureAwait(false))
                        {
                            var state = Atomic.Lock(ref _state);
                            if (_e._backpressure)
                                while (_state == _sEmitting && _queue.Count == _queue.MaxCapacity)
                                {
                                    _tsEmitting = tsEmitting;
                                    _state = _sEmitting;
                                    if (!await tsEmitting.Task.ConfigureAwait(false))
                                        return;
                                    _tsEmitting.Reset();
                                    state = Atomic.Lock(ref state);
                                }

                            switch (state)
                            {
                                case _sEmitting:
                                    try { _queue.Enqueue(item); }
                                    finally { _state = _sEmitting; }
                                    break;

                                case _sAccepting:
                                    Debug.Assert(_queue.Count == 0);
                                    Current = item;
                                    _state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                    if (_state == _sFinal)
                                        return;
                                    break;

                                default: // _sFinal
                                    _state = state;
                                    return;
                            }
                        }
                    }
                    catch (Exception ex) { error = ex; }
                    finally
                    {
                        _atmbDisposed.SetResult();

                        var state = Atomic.Lock(ref _state);
                        if (state == _sEmitting && _queue.Count > 0)
                        {
                            _error = error;
                            _state = _sCompleted;
                        }
                        else
                        {
                            _state = state;
                            SetFinal(error);
                        }
                    }
                }
            }
        }
    }
}
