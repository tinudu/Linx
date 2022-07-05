using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Group source elements by a key and build aggregates for each group.
    /// </summary>
    public static IAsyncEnumerable<KeyValuePair<TKey, TAggregate>> GroupAggregate<TSource, TKey, TAggregate>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<IAsyncGrouping<TKey, TSource>, CancellationToken, ValueTask<TAggregate>> aggregator,
        IEqualityComparer<TKey>? keyComparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

        return source
            .GroupBy(keySelector, keyComparer)
            .SelectAwait(async (g, t) => new KeyValuePair<TKey, TAggregate>(g.Key, await aggregator(g, t).ConfigureAwait(false)));
    }
}
