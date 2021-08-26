using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Linx.Queueing
{
    partial class QueueFactory
    {
        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that holds the most recent item.
        /// </summary>
        public static IQueue<T, T> MostRecent<T>() => new MostRecentOneQueue<T>();

        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that holds the <paramref name="maxSize"/> most recent items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> must be positive.</exception>
        public static IQueue<T, T> MostRecent<T>(int maxSize)
            => maxSize switch
            {
                1 => new MostRecentOneQueue<T>(),
                > 1 => new MostRecentManyQueue<T>(maxSize),
                _ => throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be positive.")
            };

        /// <summary>
        /// A queue containing the most recent item.
        /// </summary>
        private sealed class MostRecentOneQueue<T> : IQueue<T, T>
        {
            private T _item;

            public bool IsEmpty { get; private set; }

            public bool IsFull => false;

            public void Enqueue(T item)
            {
                IsEmpty = false;
                _item = item;
            }

            public T Dequeue()
            {
                if (IsEmpty) throw new InvalidOperationException("Queue is empty.");

                IsEmpty = true;
                return Linx.Clear(ref _item);
            }
        }

        /// <summary>
        /// A queue containing the most recent N items.
        /// </summary>
        private sealed class MostRecentManyQueue<T> : IQueue<T, T>
        {
            private readonly int _maxSize;
            private Queue<T> _queue = new();

            public MostRecentManyQueue(int maxSize)
            {
                Debug.Assert(maxSize > 1);
                _maxSize = maxSize;
            }

            public bool IsEmpty => _queue.Count == 0;

            public bool IsFull => false;

            public void Enqueue(T item)
            {
                while (_queue.Count >= _maxSize)
                    _queue.Dequeue();
                _queue.Enqueue(item);
            }

            public T Dequeue() => _queue.Dequeue();
        }
    }
}
