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
        public static IAsyncEnumerable<T> Next<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, NextQueue<T>.Instance, token));
        }
    }
}