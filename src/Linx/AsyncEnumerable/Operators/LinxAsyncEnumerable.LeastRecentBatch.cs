using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets access to the least recent <paramref name="maxCapacity"/> items.
        /// </summary>
        public static IAsyncEnumerable<Deferred<Lossy<IReadOnlyList<T>>>> LeastRecentBatch<T>(this IAsyncEnumerable<T> source, int maxCapacity)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            return new QueueingIterator<T, Lossy<IReadOnlyList<T>>>(source, () => new LeastRecentBatchQueue<T>(maxCapacity));
        }

        private sealed class LeastRecentBatchQueue<T> : ListQueueBase<T, Lossy<IReadOnlyList<T>>>
        {
            private int _ignoredCount;

            public LeastRecentBatchQueue(int maxCapacity) : base(maxCapacity) { }

            public override bool Backpressure => false;

            public override void Enqueue(T item)
            {
                if (IsFull)
                    checked { _ignoredCount++; }
                else
                    EnqueueThrowIfFull(item);
            }

            public override Lossy<IReadOnlyList<T>> Dequeue()
            {
                var result = new Lossy<IReadOnlyList<T>>(DequeueAll(), _ignoredCount);
                _ignoredCount = 0;
                return result;
            }

            public override void DequeueFailSafe()
            {
                Clear();
                _ignoredCount = 0;
            }
        }
    }
}
