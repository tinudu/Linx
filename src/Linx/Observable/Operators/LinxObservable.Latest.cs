namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using AsyncEnumerable;
    using Queueing;

    partial class LinxObservable
    {
        /// <summary>
        /// Ignores but the latest element if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new LatestOneQueue<T>(), token));
        }

        /// <summary>
        /// Ignores but the latest <paramref name="maxCount"/> elements if the consumer is slower than the producer.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
        public static IAsyncEnumerable<T> Latest<T>(this ILinxObservable<T> source, int maxCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

            return maxCount switch
            {
                0 => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, NextQueue<T>.Instance, token)),
                1 => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new LatestOneQueue<T>(), token)),
                int.MaxValue => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new BufferAllQueue<T>(), token)),
                _ => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new LatestMaxQueue<T>(maxCount), token)),
            };
        }
    }
}
