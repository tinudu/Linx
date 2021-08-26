using System;
using System.Collections.Generic;

namespace Linx.Queueing
{
    /// <summary>
    /// Abstraction of a queue.
    /// </summary>
    /// <typeparam name="TIn">Type of items being enqueued.</typeparam>
    /// <typeparam name="TOut">Type of items being dequeued.</typeparam>
    public interface IQueue<in TIn, out TOut>
    {
        /// <summary>
        /// Gets whether the queue is full.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Enqueue an item.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is full.</exception>
        void Enqueue(TIn item);

        /// <summary>
        /// Dequeue an item.
        /// </summary>
        /// <exception cref="InvalidOperationException">The queue is empty.</exception>
        TOut Dequeue();

        /// <summary>
        /// Dequeue all items from the queue.
        /// </summary>
        IReadOnlyList<TOut> DequeueAll();

        /// <summary>
        /// Clears all items from the queue.
        /// </summary>
        void Clear();
    }
}
