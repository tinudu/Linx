namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Queueing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Next<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(token => new QueueingEnumerator<T>(source, NextQueue<T>.Instance, token));
        }
    }
}