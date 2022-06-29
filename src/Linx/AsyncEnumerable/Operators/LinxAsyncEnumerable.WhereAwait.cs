using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Filters a sequence of values based on an async predicate.
    /// </summary>
    public static IAsyncEnumerable<T> WhereAwait<T>(
        this IAsyncEnumerable<T> source,
        Func<T, CancellationToken, Task<bool>> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                if (await predicate(item, token).ConfigureAwait(false))
                    yield return item;
        }
    }

    /// <summary>
    /// Filters a sequence of values based on an async predicate.
    /// </summary>
    public static IAsyncEnumerable<T> WhereAwait<T>(
        this IAsyncEnumerable<T> source,
        Func<T, CancellationToken, ValueTask<bool>> predicate,
        bool preserveOrder,
        int maxConcurrent = int.MaxValue)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return source
            .SelectAwait(async (x, t) => (item: x, isInFilter: await predicate(x, t).ConfigureAwait(false)), preserveOrder, maxConcurrent)
            .Where(xb => xb.isInFilter)
            .Select(xb => xb.item);
    }
}
