using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Determines whether a sequence contains any elements.
    /// </summary>
    public static async Task<bool> Any<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        return await ae.MoveNextAsync();
    }

    /// <summary>
    /// Determines whether any element of a sequence satisfies a condition.
    /// </summary>
    public static async Task<bool> Any<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        token.ThrowIfCancellationRequested();

        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            if (predicate(item))
                return true;
        return false;
    }
}
