namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Observable;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Samples <paramref name="source"/> at sampling ticks provided by <paramref name="sampler"/>.
        /// </summary>
        public static ILinxObservable<TSource> Sample<TSource, TSample>(this IAsyncEnumerable<TSource> source, ILinxObservable<TSample> sampler)
            => source.ToLinxObservable().Sample(sampler);

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static ILinxObservable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
            => source.ToLinxObservable().Sample(interval);

        /// <summary>
        /// Samples <paramref name="source"/> at the specified interval.
        /// </summary>
        public static ILinxObservable<T> Sample<T>(this IAsyncEnumerable<T> source, int intervalMilliseconds)
            => source.ToLinxObservable().Sample(intervalMilliseconds);
    }
}
