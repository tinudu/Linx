using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        private interface IQueue<in TSource, TResult>
        {
            /// <summary>
            /// If true signals that the queue is full and <see cref="Enqueue(TSource)"/> should not be called.
            /// </summary>
            bool Backpressure { get; }

            /// <summary>
            /// Enqueue an item; may throw.
            /// </summary>
            void Enqueue(TSource item);

            /// <summary>
            /// Gets whether the queue is empty.
            /// </summary>
            bool IsEmpty { get; }

            /// <summary>
            /// Dequeue; may throw.
            /// </summary>
            TResult Dequeue();

            /// <summary>
            /// Dequeue without returning a result. Doesn't throw unles empty.
            /// </summary>
            void DequeueFailSafe();
        }

        private abstract class QueueBase<TSource, TQueue, TResult> : IQueue<TSource, TResult>
        {
            private readonly int _maxCapacity;
            private readonly int _initialCapacity;

            private TQueue[] _buffer;
            private int _offset, _count;

            protected QueueBase(int maxCapacity, bool initialCapacity1)
            {
                if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));

                _maxCapacity = maxCapacity;

                if (initialCapacity1)
                    _initialCapacity = 1;
                else
                {
                    _initialCapacity = maxCapacity;
                    while (_initialCapacity > 7)
                        _initialCapacity >>= 1;
                }
            }

            protected TQueue[] Buffer => _buffer;
            protected int Offset => _offset;
            public bool IsEmpty => _count == 0;
            public bool IsFull => _count == _maxCapacity;

            public abstract bool Backpressure { get; }
            public abstract void Enqueue(TSource item);
            public abstract TResult Dequeue();
            public abstract void DequeueFailSafe();

            protected void EnqueueThrowIfFull(TQueue item)
            {
                if (IsFull) throw new InvalidOperationException(Strings.QueueIsFull);

                if (_buffer is null)
                {
                    Debug.Assert(_offset == 0 && _count == 0);

                    _buffer = new TQueue[_initialCapacity];
                    _buffer[0] = item;
                    _count = 1;
                }
                else if (_count == _buffer.Length)
                {
                    var s = _maxCapacity;
                    while (s > 7)
                    {
                        var s1 = s >> 1;
                        if (s1 <= _buffer.Length)
                            break;
                        s = s1;
                    }
                    var b = new TQueue[s];
                    if (_offset == 0)
                        Array.Copy(_buffer, b, _count);
                    else
                    {
                        var c0 = _buffer.Length - _offset;
                        Array.Copy(_buffer, _offset, b, 0, c0);
                        Array.Copy(_buffer, 0, b, c0, _count - c0);
                        _offset = 0;
                    }
                    _buffer = b;
                    b[_count++] = item;
                }
                else if (_offset == 0)
                    _buffer[_count++] = item;
                else
                {
                    var ix = _offset - _buffer.Length + _count;
                    _buffer[ix >= 0 ? ix : ix + _buffer.Length] = item;
                    _count++;
                }
            }

            protected TQueue DequeueOne()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                var result = Linx.Clear(ref _buffer[_offset++]);
                if (--_count == 0)
                {
                    _offset = 0;
                    if (_buffer.Length < _initialCapacity)
                        _buffer = null;
                }
                else if (_offset == _buffer.Length)
                    _offset = 0;
                return result;
            }

            protected IReadOnlyCollection<TQueue> DequeueAll()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                IReadOnlyCollection<TQueue> result;
                if (_offset == 0)
                {
                    if (_count == _buffer.Length)
                        result = _buffer;
                    else
                        result = new ArraySegment<TQueue>(_buffer, 0, _count);
                }
                else
                {
                    var c0 = _buffer.Length - _offset;
                    var c1 = _count - c0;
                    if (c1 < 0)
                        result = new ArraySegment<TQueue>(_buffer, _offset, _count);
                    else
                        result = new ConcatArraySegments<TQueue>(new ArraySegment<TQueue>(_buffer, _offset, c0), new ArraySegment<TQueue>(_buffer, 0, c1));
                }

                _buffer = null;
                _offset = _count = 0;
                return result;
            }

            protected void Clear()
            {
                _buffer = null;
                _offset = _count = 0;
            }
        }

        private abstract class ListQueueBase<TSource, TResult> : IQueue<TSource, TResult>
        {
            private readonly int _maxCapacity;
            private TSource[] _buffer;
            private int _count;

            protected ListQueueBase(int maxCapacity)
            {
                if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));
                _maxCapacity = maxCapacity;
            }

            public bool IsFull => _count == _maxCapacity;
            public abstract bool Backpressure { get; }
            public abstract void Enqueue(TSource item);

            public bool IsEmpty => _count == 0;
            public abstract TResult Dequeue();
            public abstract void DequeueFailSafe();

            protected void EnqueueThrowIfFull(TSource item)
            {
                if (IsFull) throw new InvalidOperationException(Strings.QueueIsFull);

                if (_buffer is null)
                {
                    Debug.Assert(_count == 0);
                    _buffer = new TSource[] { item };
                    _count = 1;
                }
                else
                {
                    if (_count == _buffer.Length) // increase buffer size
                    {
                        var s = _maxCapacity;
                        while (s > 7)
                        {
                            var s1 = s >> 1;
                            if (s1 <= _buffer.Length)
                                break;
                            s = s1;
                        }
                        var b = new TSource[s];
                        Array.Copy(_buffer, b, _count);
                        _buffer = b;
                    }
                    _buffer[_count++] = item;
                }
            }

            protected IReadOnlyList<TSource> DequeueAll()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                IReadOnlyList<TSource> result = _count == _buffer.Length ? _buffer : new ArraySegment<TSource>(_buffer, 0, _count);
                _buffer = null;
                _count = 0;
                return result;
            }

            protected void Clear()
            {
                _buffer = null;
                _count = 0;
            }
        }

        private sealed class ConcatArraySegments<T> : IReadOnlyCollection<T>, ICollection<T>
        {
            private readonly ArraySegment<T> _seg0, _seg1;

            public ConcatArraySegments(ArraySegment<T> seg0, ArraySegment<T> seg1)
            {
                _seg0 = seg0;
                _seg1 = seg1;
                Count = _seg0.Count + _seg1.Count;
            }

            public int Count { get; }

            bool ICollection<T>.IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in _seg0)
                    yield return item;
                foreach (var item in _seg1)
                    yield return item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(T[] array, int arrayIndex)
            {
                if (array is null) throw new ArgumentNullException(nameof(array));
                if (arrayIndex <= 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Is less than 0.");
                if (array.Length - arrayIndex < Count) throw new ArgumentException("The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array.");

                _seg0.CopyTo(array, arrayIndex);
                _seg1.CopyTo(array, arrayIndex + _seg0.Count);
            }

            bool ICollection<T>.Contains(T item) => ((ICollection<T>)_seg0).Contains(item) || ((ICollection<T>)_seg1).Contains(item);

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
        }

        private sealed class QueueingIterator<TSource, TResult> : IAsyncEnumerable<DeferredDequeue<TResult>>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<IQueue<TSource, TResult>> _queueFactory;

            public QueueingIterator(IAsyncEnumerable<TSource> source, Func<IQueue<TSource, TResult>> queueFactory)
            {
            }

            public IAsyncEnumerator<DeferredDequeue<TResult>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
