using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static IAsyncEnumerable<T> ToAsync<T>(this IEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        // we use a CoroutineIterator rather than an async iterator because it handles cancellation.
        return Create<T>(async (yield, _) =>
        {
            foreach (var item in source)
                if (!await yield(item))
                    return;
        });
    }
}
