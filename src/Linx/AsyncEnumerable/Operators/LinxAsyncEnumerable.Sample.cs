using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Samples <paramref name="source"/> at sampling ticks provided by <paramref name="sampler"/>.
    /// </summary>
    public static IAsyncEnumerable<TSource> Sample<TSource, TSample>(this IAsyncEnumerable<TSource> source, IAsyncEnumerable<TSample> sampler)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (sampler == null) throw new ArgumentNullException(nameof(sampler));

        return source.Latest().Zip(sampler.Latest(), (x, _) => x.GetResult());
    }

    /// <summary>
    /// Samples <paramref name="source"/> at the specified interval.
    /// </summary>
    public static IAsyncEnumerable<T> Sample<T>(this IAsyncEnumerable<T> source, TimeSpan period)
        => source.Sample(Interval(period));
}
