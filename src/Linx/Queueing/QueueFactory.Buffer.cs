using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linx.Queueing
{
    partial class QueueFactory
    {
        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that buffers all items.
        /// </summary>
        public static IQueue<T, T> Buffer<T>() => new BufferAllQueue<T>();

        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that buffers up to <paramref name="maxSize"/> items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> must be positive.</exception>
        public static IQueue<T, T> Buffer<T>(int maxSize)
            => maxSize switch
            {
                int.MaxValue => new BufferAllQueue<T>(),
                > 0 => new BufferNQueue<T>(maxSize),
                _ => throw new ArgumentOutOfRangeException(nameof(maxSize))
            };

        private sealed class BufferAllQueue<T> : IQueue<T, T>
        {
            private readonly Queue<T> _queue = new();

            bool IQueue<T, T>.IsFull => false;

            public bool IsEmpty => _queue.Count == 0;

            public void Enqueue(T item) => _queue.Enqueue(item);

            public T Dequeue() => _queue.Dequeue();

            public IReadOnlyList<T> DequeueAll()
            {
                var result = _queue.ToList();
                _queue.Clear();
                return result;
            }

            public void Clear() => _queue.Clear();
        }

        private sealed class BufferNQueue<T> : IQueue<T, T>
        {
            private readonly Queue<T> _queue = new();
            private readonly int _maxSize;

            public BufferNQueue(int maxSize)
            {
                Debug.Assert(maxSize > 0 && maxSize < int.MaxValue);
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

            public IReadOnlyList<T> DequeueAll()
            {
                var result = _queue.ToList();
                _queue.Clear();
                return result;
            }

            public void Clear() => _queue.Clear();
        }
    }
}
