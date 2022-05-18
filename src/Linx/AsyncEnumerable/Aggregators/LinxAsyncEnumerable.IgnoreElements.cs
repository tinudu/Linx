using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Consumes <paramref name="source"/> ignoring its elements.
    /// </summary>
    public static async Task IgnoreElements<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        await foreach (var _ in source.WithCancellation(token).ConfigureAwait(false))
        {
        }
    }
}
