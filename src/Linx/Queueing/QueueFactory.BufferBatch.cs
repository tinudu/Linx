using System;
using System.Collections.Generic;

namespace Linx.Queueing
{
    partial class QueueFactory
    {
        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that buffers all elements into a <see cref="IList{T}"/>.
        /// </summary>
        public static IQueue<T, IList<T>> BufferBatch<T>() => new BufferBatchQueue<T>(int.MaxValue);

        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that buffers up to <paramref name="maxSize"/> items into a <see cref="IList{T}"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> must greater than 1.</exception>
        public static IQueue<T, IList<T>> BufferBatch<T>(int maxSize) => new BufferBatchQueue<T>(maxSize);

        private sealed class BufferBatchQueue<T> : IQueue<T, IList<T>>
        {
            private readonly int _maxSize;
            private IList<T> _list;

            public BufferBatchQueue(int maxSize)
            {
                if (maxSize <= 1) throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be > 1.");
                _maxSize = maxSize;
            }

            public bool IsFull => _list is not null && _list.Count >= _maxSize;

            public bool IsEmpty => _list == null;

            public void Enqueue(T item)
            {
                switch (_list)
                {
                    case null:
                        _list = new[] { item };
                        break;
                    case T[] a:
                        _list = new List<T>(4) { a[0], item };
                        break;
                    default:
                        if (_list.Count >= _maxSize) throw new InvalidOperationException("Queue is full.");
                        _list.Add(item);
                        break;
                }
            }

            public IList<T> Dequeue()
            {
                if (_list == null) throw new InvalidOperationException("Queue is empty.");
                return Linx.Clear(ref _list);
            }
        }
    }
}
