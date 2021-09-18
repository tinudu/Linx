using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets access to the most recent <paramref name="maxCapacity"/> items.
        /// </summary>
        public static IAsyncEnumerable<Deferred<Lossy<IReadOnlyCollection<T>>>> MostRecentBatch<T>(this IAsyncEnumerable<T> source, int maxCapacity)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            return new QueueingIterator<T, Lossy<IReadOnlyCollection<T>>>(source, () => new MostRecentBatchQueue<T>(maxCapacity));
        }

        private sealed class MostRecentBatchQueue<T> : QueueBase<T, T, Lossy<IReadOnlyCollection<T>>>
        {
            private int _ignoredCount;

            public MostRecentBatchQueue(int maxCapacity) : base(maxCapacity, true) { }

            public override bool Backpressure => false;

            public override void Enqueue(T item)
            {
                if (IsFull)
                {
                    DequeueOne();
                    checked { _ignoredCount++; }
                }

                EnqueueThrowIfFull(item);
            }

            public override Lossy<IReadOnlyCollection<T>> Dequeue()
            {
                var result = new Lossy<IReadOnlyCollection<T>>(DequeueAll(), _ignoredCount);
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
