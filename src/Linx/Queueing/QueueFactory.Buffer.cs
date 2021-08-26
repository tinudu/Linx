using System;
using System.Collections.Generic;

namespace Linx.Queueing
{
    partial class QueueFactory
    {
        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that buffers all elements.
        /// </summary>
        public static IQueue<T, T> Buffer<T>() => new BufferQueue<T>(int.MaxValue);

        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that buffers up to <paramref name="maxSize"/> elements.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> must greater than 1.</exception>
        public static IQueue<T, T> Buffer<T>(int maxSize) => new BufferQueue<T>(maxSize);

        private sealed class BufferQueue<T> : IQueue<T, T>
        {
            private readonly Queue<T> _queue = new();
            private readonly int _maxSize;

            public BufferQueue(int maxSize)
            {
                if (maxSize <= 1) throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be > 1.");
                _maxSize = maxSize;
            }

            public bool IsFull => _queue.Count >= _maxSize;

            public bool IsEmpty => _queue.Count == 0;

            public void Enqueue(T item)
            {
                if (_queue.Count >= _maxSize) throw new InvalidOperationException("Queue is full.");
                _queue.Enqueue(item);
            }

            public T Dequeue() => _queue.Dequeue();
        }
    }
}
