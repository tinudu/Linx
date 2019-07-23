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
            return source.Latest().Zip(sampler.Latest(), (x, y) => x);
        }

        /// <summary>
        /// Samples <paramref name="source"/> every <paramref name="interval"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Latest().Zip(Interval(interval).Latest(), (x, y) => x);
        }
    }
}
