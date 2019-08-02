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
            => source.Latest().Zip(sampler.Latest(), (x, y) => x).WithName();

        /// <summary>
        /// Samples <paramref name="source"/> every <paramref name="interval"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
            => source.Sample(Interval(interval));
    }
}
