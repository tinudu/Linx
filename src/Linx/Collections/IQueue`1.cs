namespace Linx.Collections
{
    using System.Collections.Generic;

    /// <summary>
    /// Some kind of queue.
    /// </summary>
    public interface IQueue<T>
    {
        /// <summary>
        /// Gets the number of items in the queue.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets whether the queue is full.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Enqueue an item.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The queue is full.</exception>
        /// <exception cref="System.Exception">Error caused by allocating memory.</exception>
        void Enqueue(T item);

        /// <summary>
        /// Gets the next item without dequeueing it.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The queue is empty.</exception>
        T Peek();

        /// <summary>
        /// Dequeues the next item.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The queue is empty.</exception>
        T Dequeue();

        /// <summary>
        /// Dequeues all items.
        /// </summary>
        IReadOnlyCollection<T> DequeueAll();

        /// <summary>
        /// Clears the queue.
        /// </summary>
        void Clear();
    }
}
