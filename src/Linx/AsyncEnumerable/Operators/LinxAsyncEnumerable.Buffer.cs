namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Buffers all elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(token => new BufferEnumerator<T>(source, int.MaxValue, token));
        }

        /// <summary>
        /// Buffers up to <paramref name="maxCount"/> elements if the consumer is slower than the producer.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
        /// <remarks>
        /// When the buffer is full and another element is notified, the producer experiences backpressure.
        /// </remarks>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return maxCount <= 0 ?
                source :
                Create(token => new BufferEnumerator<T>(source, maxCount, token));
        }

        private sealed class BufferEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sDisposing = 3;
            private const int _sLast = 4;
            private const int _sFinal = 5;

            private readonly IAsyncEnumerable<T> _source;
            private readonly IQueue _queue;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private ManualResetValueTaskSource<bool> _tsQueueFull;
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private int _state;
            private Exception _error;

            public BufferEnumerator(IAsyncEnumerable<T> source, int maxCount, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(maxCount > 0);

                _source = source;
                _token = token;
                _queue = maxCount == int.MaxValue ? (IQueue)new InfiniteQueue() : new MaxQueue(maxCount);

                if (token.CanBeCanceled) _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
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
                        if (_queue.IsEmpty)
                            _state = _sAccepting;
                        else
                        {
                            var tsQueueFull = Linx.Clear(ref _tsQueueFull);
                            Current = _queue.Dequeue();
                            if (_queue.IsEmpty)
                                try { _queue.TrimExcess(); }
                                catch {/**/}
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                            tsQueueFull?.SetResult(true);
                        }
                        break;

                    case _sLast:
                        Debug.Assert(!_queue.IsEmpty);
                        Current = _queue.Dequeue();
                        if (_queue.IsEmpty)
                        {
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sLast;
                        _tsAccepting.SetResult(true);
                        break;

                    default:
                        Debug.Assert(state == _sDisposing || state == _sFinal);
                        _state = state;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;
                }
                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                Dispose(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void Dispose(Exception error)
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
                        _error = error;
                        _state = _sDisposing;
                        _ctr.Dispose();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        var tsQueueFull = Linx.Clear(ref _tsQueueFull);
                        _error = error;
                        _state = _sDisposing;
                        _ctr.Dispose();
                        _queue.Clear();
                        tsQueueFull?.SetResult(false);
                        break;

                    case _sLast:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _queue.Clear();
                        _atmbDisposed.SetResult();
                        break;

                    default:
                        Debug.Assert(state == _sDisposing || state == _sFinal);
                        _state = state;
                        break;
                }
            }

            private async void Produce()
            {
                Exception error = null;
                try
                {
                    _token.ThrowIfCancellationRequested();

                    var tsQueueFull = new ManualResetValueTaskSource<bool>();

                    await foreach (var item in _source.WithCancellation(_token).ConfigureAwait(false))
                    {
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

                                    try { _queue.Enqueue(item); }
                                    finally { _state = _sEmitting; }
                                    break;

                                default:
                                    Debug.Assert(state == _sDisposing);
                                    _state = _sDisposing;
                                    return;
                            }
                            break;
                        }
                    }
                }
                catch (Exception ex) { error = ex; }
                finally
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetExceptionOrResult(error, false);
                            break;

                        case _sEmitting:
                            _error = error;
                            if (_queue.IsEmpty)
                            {
                                _state = _sFinal;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sLast;
                            break;

                        default:
                            Debug.Assert(state == _sDisposing);
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;
                    }
                }
            }

            private interface IQueue
            {
                bool IsEmpty { get; }
                bool IsFull { get; }
                void Enqueue(T item);
                T Dequeue();
                void Clear();
                void TrimExcess();
            }

            private sealed class InfiniteQueue : Queue<T>, IQueue
            {
                public bool IsEmpty => Count == 0;
                public bool IsFull => false;
            }

            private sealed class MaxQueue : Queue<T>, IQueue
            {
                private readonly int _maxCount;

                public MaxQueue(int maxCount)
                {
                    Debug.Assert(maxCount > 1 && maxCount < int.MaxValue);
                    _maxCount = maxCount;
                }

                public bool IsEmpty => Count == 0;
                public bool IsFull => Count == _maxCount;
            }
        }
    }
}
