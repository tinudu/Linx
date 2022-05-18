using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns the number of elements in a sequence.
    /// </summary>
    public static async Task<long> LongCount<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        var count = 0L;
        await foreach (var _ in source.WithCancellation(token).ConfigureAwait(false))
            checked { count++; }
        return count;
    }

    /// <summary>
    /// Returns a number that represents how many elements in the specified sequence satisfy a condition.
    /// </summary>
    public static async Task<long> LongCount<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        => await source.Where(predicate).LongCount(token).ConfigureAwait(false);
}
