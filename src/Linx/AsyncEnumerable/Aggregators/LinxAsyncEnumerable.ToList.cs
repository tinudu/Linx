using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Aggregate elements into a list.
    /// </summary>
    public static async ValueTask<List<T>> ToList<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var result = new List<T>();
        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            result.Add(item);

        return result;
    }
}
