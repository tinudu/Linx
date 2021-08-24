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
        /// Ignores but the latest element if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(token => new LatestEnumerator<T>(source, 1, token));
        }

        /// <summary>
        /// Ignores but the latest <paramref name="maxCount"/> elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source, int maxCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return maxCount <= 0 ?
                source.Next() :
                Create(token => new LatestEnumerator<T>(source, maxCount, token));
        }

        private sealed class LatestEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmittingMutable = 2;
            private const int _sEmittingReadOnly = 3;
            private const int _sDisposing = 4;
            private const int _sLast = 5;
            private const int _sFinal = 6;

            private readonly IAsyncEnumerable<T> _source;
            private readonly IQueue _queue;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private T _current;
            private int _state;
            private Exception _error;

            public LatestEnumerator(IAsyncEnumerable<T> source, int maxCount, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(maxCount >= 0);

                _source = source;
                _token = token;
                _queue = maxCount switch
                {
                    0 => (IQueue)ZeroQueue.Instance,
                    1 => new OneQueue(),
                    int.MaxValue => new InfiniteQueue(),
                    _ => new MaxQueue(maxCount)
                };

                if (token.CanBeCanceled) _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
            }

            public T Current
            {
                get
                {
                    Atomic.CompareExchange(ref _state, _sEmittingReadOnly, _sEmittingMutable);
                    return _current;
                }
            }

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

                    case _sEmittingMutable:
                    case _sEmittingReadOnly:
                        if (_queue.IsEmpty)
                            _state = _sAccepting;
                        else
                        {
                            _current = _queue.Dequeue();
                            _state = _sEmittingMutable;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sLast:
                        Debug.Assert(!_queue.IsEmpty);
                        _current = _queue.Dequeue();
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

                    case _sEmittingMutable:
                    case _sEmittingReadOnly:
                        _error = error;
                        _state = _sDisposing;
                        _ctr.Dispose();
                        _queue.Clear();
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

                    await foreach (var item in _source.WithCancellation(_token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                _current = item;
                                _state = _sEmittingMutable;
                                _tsAccepting.SetResult(true);
                                break;

                            case _sEmittingMutable:
                                _queue.Enqueue(item, ref _current, out _state);
                                break;

                            case _sEmittingReadOnly:
                                _queue.Enqueue(item, out _state);
                                break;

                            default:
                                Debug.Assert(state == _sDisposing);
                                _state = _sDisposing;
                                return;
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

                        case _sEmittingMutable:
                        case _sEmittingReadOnly:
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
                void Enqueue(T item, ref T current, out int state);
                void Enqueue(T item, out int state);
                T Dequeue();
                void Clear();
            }

            private sealed class ZeroQueue : IQueue
            {
                public static ZeroQueue Instance { get; } = new ZeroQueue();
                private ZeroQueue() { }

                public bool IsEmpty => true;

                // ReSharper disable once RedundantAssignment
                public void Enqueue(T item, ref T current, out int state)
                {
                    current = item;
                    state = _sEmittingMutable;
                }

                public void Enqueue(T item, out int state) => state = _sEmittingReadOnly;

                public T Dequeue() => throw new InvalidOperationException();

                public void Clear() { }
            }

            private sealed class OneQueue : IQueue
            {
                private T _item;

                public bool IsEmpty { get; private set; } = true;

                // ReSharper disable once RedundantAssignment
                public void Enqueue(T item, ref T current, out int state)
                {
                    Debug.Assert(IsEmpty);
                    current = item;
                    state = _sEmittingMutable;
                }

                public void Enqueue(T item, out int state)
                {
                    IsEmpty = false;
                    _item = item;
                    state = _sEmittingReadOnly;
                }

                public T Dequeue()
                {
                    Debug.Assert(!IsEmpty);
                    IsEmpty = true;
                    return _item;
                }

                public void Clear() => IsEmpty = true;
            }

            private sealed class InfiniteQueue : Queue<T>, IQueue
            {
                public bool IsEmpty => Count == 0;

                public void Enqueue(T item, ref T current, out int state)
                {
                    try { Enqueue(item); }
                    finally { state = _sEmittingMutable; }
                }

                public void Enqueue(T item, out int state)
                {
                    try { Enqueue(item); }
                    finally { state = _sEmittingReadOnly; }
                }
            }

            private sealed class MaxQueue : Queue<T>, IQueue
            {
                private readonly int _maxCount, _maxCountM1;

                public MaxQueue(int maxCount)
                {
                    Debug.Assert(maxCount > 1 && maxCount < int.MaxValue);
                    _maxCount = maxCount;
                    _maxCountM1 = maxCount - 1;
                }

                public bool IsEmpty => Count == 0;

                public void Enqueue(T item, ref T current, out int state)
                {
                    Debug.Assert(Count <= _maxCountM1);
                    try
                    {
                        if (Count == _maxCountM1)
                            current = Dequeue();
                        Enqueue(item);
                    }
                    finally { state = _sEmittingMutable; }
                }

                public void Enqueue(T item, out int state)
                {
                    Debug.Assert(Count <= _maxCount);
                    try
                    {
                        if (Count == _maxCount)
                            Dequeue();
                        Enqueue(item);
                    }
                    finally { state = _sEmittingReadOnly; }
                }
            }
        }
    }
}