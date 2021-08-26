﻿using System.Collections.Generic;

namespace Linx.Queueing
{
    /// <summary>
    /// Provides access to a queue.
    /// </summary>
    public struct QueueReader<T>
    {
        /// <summary>
        /// Object providing the implementation.
        /// </summary>
        public interface IProvider
        {
            /// <summary>
            /// Implementation of <see cref="QueueReader{T}.Dequeue"/>.
            /// </summary>
            T Dequeue(short version);

            /// <summary>
            /// Implementation of <see cref="QueueReader{T}.DequeueAll"/>.
            /// </summary>
            IReadOnlyList<T> DequeueAll(short version);
        }

        private readonly IProvider _provider;
        private readonly short _version;

        /// <summary>
        /// Initialize.
        /// </summary>
        public QueueReader(IProvider provider, short version)
        {
            _provider = provider;
            _version = version;
        }

        /// <summary>
        /// Dequeue one item from the queue.
        /// </summary>
        public T Dequeue() => _provider.Dequeue(_version);

        /// <summary>
        /// Dequeue all items from the queue.
        /// </summary>
        public IReadOnlyList<T> DequeueAll() => _provider.DequeueAll(_version);
    }
}
