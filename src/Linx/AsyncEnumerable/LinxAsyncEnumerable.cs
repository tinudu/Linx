using System.Collections.Generic;
using System;

namespace Linx.AsyncEnumerable
{
    /// <summary>
    /// Static Linx.AsyncEnumerable methods.
    /// </summary>
    public static partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Proxy to a queue.
        /// </summary>
        /// <remarks>
        /// A <see cref="DeferredDequeue{T}"/> value is exposed to <see cref="IAsyncEnumerator{T}.Current"/> 
        /// after <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> returns true.
        /// It allows for at most one dequeue operation, which has to happen before the next call
        /// to <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> or <see cref="IAsyncDisposable.DisposeAsync"/>.
        /// </remarks>
        public struct DeferredDequeue<T>
        {
            internal interface IProvider
            {
                T Dequeue(short version);
            }

            private readonly IProvider _provider;
            private readonly short _version;

            internal DeferredDequeue(IProvider provider, short version)
            {
                _provider = provider;
                _version = version;
            }

            /// <summary>
            /// Dequeue one item.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            /// <see cref="Dequeue"/> was called before or the <see cref="IAsyncEnumerator{T}"/> has advanced.
            /// </exception>
            public T Dequeue() => _provider is null ? default : _provider.Dequeue(_version);
        }

        /// <summary>
        /// Encapsulates the result of a lossy dequeue operation.
        /// </summary>
        public struct Lossy<T>
        {
            /// <summary>
            /// Gets the dequeued value.
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// Gets the number of ignored items before (MostRecent) or after (LeastRecent) the value was dequeued.
            /// </summary>
            public int IgnoredCount { get; }

            internal Lossy(T value, int ignoredCount)
            {
                Value = value;
                IgnoredCount = ignoredCount;
            }
        }
    }
}
