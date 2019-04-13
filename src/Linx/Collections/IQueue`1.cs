namespace Linx.Collections
{
    /// <summary>
    /// Some kind of queue.
    /// </summary>
    public interface IQueue<T>
    {
        /// <summary>
        /// Gets whether the queue is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Enqueue an item.
        /// </summary>
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
        /// Clears the queue.
        /// </summary>
        void Clear();
    }
}
