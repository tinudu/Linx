namespace Linx.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Provides a set of factory methods for <see cref="IQueue{T}"/>.
    /// </summary>
    public static class Queue
    {
        /// <summary>
        /// Gets a queue that is always empty (ignoring enqueued items).
        /// </summary>
        public static IQueue<T> Empty<T>() => EmptyQueue<T>.Singleton;

        /// <summary>
        /// Gets a queue that stores all items.
        /// </summary>
        public static IQueue<T> All<T>() => new AllQueue<T>();

        /// <summary>
        /// Gets a queue that stores just the latest item.
        /// </summary>
        public static IQueue<T> Latest<T>() => new LatestOne<T>();

        /// <summary>
        /// Gets a <see cref="IQueue{T}"/> that stores the latest <paramref name="maxSize"/> items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> is negative.</exception>
        public static IQueue<T> Latest<T>(int maxSize)
        {
            if (maxSize < 0) throw new ArgumentOutOfRangeException(nameof(maxSize));
            switch (maxSize)
            {
                case 0: return EmptyQueue<T>.Singleton;
                case 1: return new LatestOne<T>();
                case int.MaxValue: return new AllQueue<T>();
                default: return new LatestMany<T>(maxSize);
            }
        }

        private sealed class EmptyQueue<T> : IQueue<T>
        {
            public static EmptyQueue<T> Singleton { get; } = new EmptyQueue<T>();
            private EmptyQueue() { }

            public bool IsEmpty => true;
            public void Enqueue(T item) { }
            public T Peek() => throw new InvalidOperationException(Strings.QueueIsEmpty);
            public T Dequeue() => throw new InvalidOperationException(Strings.QueueIsEmpty);
            public void Clear() { }
        }

        private sealed class AllQueue<T> : IQueue<T>
        {
            private readonly Queue<T> _queue = new Queue<T>();

            public bool IsEmpty => _queue.Count == 0;

            public void Enqueue(T item) => _queue.Enqueue(item);

            public T Peek() => _queue.Peek();

            public T Dequeue()
            {
                var item = _queue.Dequeue();
                if (_queue.Count == 0) _queue.TrimExcess();
                return item;
            }

            public void Clear()
            {
                _queue.Clear();
                _queue.TrimExcess();
            }
        }

        private sealed class LatestOne<T> : IQueue<T>
        {
            private bool _hasItem;
            private T _item;

            public bool IsEmpty => !_hasItem;

            public void Enqueue(T item)
            {
                _item = item;
                _hasItem = true;
            }

            public T Peek()
            {
                if (!_hasItem) throw new InvalidOperationException(Strings.QueueIsEmpty);
                return _item;
            }

            public T Dequeue()
            {
                if (!_hasItem) throw new InvalidOperationException(Strings.QueueIsEmpty);
                var item = _item;
                _item = default;
                _hasItem = false;
                return item;
            }

            public void Clear()
            {
                _item = default;
                _hasItem = false;
            }
        }

        private sealed class LatestMany<T> : IQueue<T>
        {
            private readonly int _maxSize, _initialSize;
            private int _deq, _enq; // indexes of dequeue and enqueue positions
            private T[] _buffer;

            public LatestMany(int maxSize)
            {
                Debug.Assert(maxSize >= 2 && maxSize < int.MaxValue);

                _initialSize = _maxSize = maxSize;

                // choose an initial size that won't get us too close to maxSize when doubling
                while (_initialSize >= 7) _initialSize = (_initialSize + 1) / 2;
            }

            public bool IsEmpty { get; private set; } = true;

            public void Enqueue(T item)
            {
                if (IsEmpty)
                {
                    if (_buffer == null) _buffer = new T[_initialSize];
                    _buffer[0] = item;
                    _deq = 0;
                    _enq = 1;
                    IsEmpty = false;
                    return;
                }

                if (_deq == _enq) // full
                {
                    if (_buffer.Length == _maxSize) // overwrite least recent
                    {
                        _buffer[_enq] = item;
                        if (++_enq == _buffer.Length) _enq = 0;
                        _deq = _enq;
                        return;
                    }

                    // increse buffer size
                    var newSize = _buffer.Length >= _maxSize / 2 ? _maxSize : 2 * _buffer.Length;
                    var newBuffer = new T[newSize];
                    var count = _buffer.Length - _deq;
                    Array.Copy(_buffer, _deq, newBuffer, 0, count);
                    Array.Copy(_buffer, 0, newBuffer, count, _deq);
                    _deq = 0;
                    _enq = _buffer.Length;
                    _buffer = newBuffer;
                }
                _buffer[_enq] = item;
                if (++_enq == _buffer.Length) _enq = 0;
            }

            public T Peek()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);
                return _buffer[_deq];
            }

            public T Dequeue()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                var result = ReadAndClear(ref _buffer[_deq]);
                if (++_deq == _buffer.Length) _deq = 0;
                if (_deq != _enq) return result;
                IsEmpty = true;
                if (_buffer.Length > _initialSize) _buffer = null;
                return result;
            }

            public void Clear()
            {
                if (IsEmpty) return;

                if (_buffer.Length > _initialSize)
                    _buffer = null;
                else
                    Array.Clear(_buffer, 0, _buffer.Length);

                IsEmpty = true;
            }

            private static T ReadAndClear(ref T item)
            {
                var result = item;
                item = default;
                return result;
            }
        }
    }
}
