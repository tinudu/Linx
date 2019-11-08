namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Next<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(token => new LatestEnumerator<T>(source, 0, token));
        }
    }
}