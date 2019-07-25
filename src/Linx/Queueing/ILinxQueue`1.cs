namespace Linx.Queueing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Abstraction of a FIFO queue.
    /// </summary>
    public interface ILinxQueue<T>
    {
        /// <summary>
        /// Gets the number of values in the queue.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Try to enqueue a value.
        /// </summary>
        /// <returns>Whether the item was enqueued, i.e. the queue was not full before the operation.</returns>
        /// <exception cref="Exception">May fail due to memory allocation problems.</exception>
        bool TryEnqueue(T value);

        /// <summary>
        /// Try to dequeue a value.
        /// </summary>
        /// <returns>The dequeued value, if there was any.</returns>
        Maybe<T> TryDequeue();

        /// <summary>
        /// Dequeue and return all values.
        /// </summary>
        IReadOnlyCollection<T> DequeueAll();

        /// <summary>
        /// Dequeue all values and return a collection of their respective projection.
        /// </summary>
        IReadOnlyCollection<TResult> DequeueAll<TResult>(Func<T, TResult> selector);

        /// <summary>
        /// Removes all items from the queue.
        /// </summary>
        void Clear();
    }
}
