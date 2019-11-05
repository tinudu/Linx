namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Samples <paramref name="source"/> at sampling ticks provided by <paramref name="sampler"/>.
        /// </summary>
        public static IAsyncEnumerable<TSource> Sample<TSource, TSample>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TSample> sampler)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampler == null) throw new ArgumentNullException(nameof(sampler));
            return sampler.Next().Zip(source.Latest(), (_, item) => item);
        }

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static IAsyncEnumerable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var sampler = Interval(interval);
            return sampler.Next().Zip(source.Latest(), (_, item) => item);
        }
    }
}
