using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Linx.Queueing
{
    partial class QueueFactory
    {
        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that holds the most recent item.
        /// </summary>
        public static IQueue<T, T> MostRecent<T>() => new MostRecent1Queue<T>();

        /// <summary>
        /// Creates a <see cref="IQueue{TIn, TOut}"/> that holds the <paramref name="maxSize"/> most recent items.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> must be positive.</exception>
        public static IQueue<T, T> MostRecent<T>(int maxSize)
            => maxSize switch
            {
                1 => new MostRecent1Queue<T>(),
                > 1 => new MostRecentNQueue<T>(maxSize),
                _ => throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be positive.")
            };

        /// <summary>
        /// A queue containing the most recent item.
        /// </summary>
        private sealed class MostRecent1Queue<T> : IQueue<T, T>
        {
            private T _item;

            public bool IsEmpty { get; private set; }

            bool IQueue<T, T>.IsFull => false;

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

            public IReadOnlyList<T> DequeueAll()
            {
                if (IsEmpty)
                    return Array.Empty<T>();
                else
                {
                    IsEmpty = true;
                    return new T[] { Linx.Clear(ref _item) };
                }
            }

            public void Clear()
            {
                IsEmpty = true;
                _item = default;
            }
        }

        /// <summary>
        /// A queue containing the most recent N items.
        /// </summary>
        private sealed class MostRecentNQueue<T> : IQueue<T, T>
        {
            // TODO: make an optimized version instead of delegating to Queue<T>.

            private readonly int _maxSize;
            private Queue<T> _queue = new();

            public MostRecentNQueue(int maxSize)
            {
                Debug.Assert(maxSize > 1);
                _maxSize = maxSize;
            }

            public bool IsEmpty => _queue.Count == 0;

            bool IQueue<T, T>.IsFull => false;

            public void Enqueue(T item)
            {
                while (_queue.Count >= _maxSize)
                    _queue.Dequeue();
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
