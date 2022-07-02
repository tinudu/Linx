using System;
using System.Collections.Generic;
using Linx.Observable;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets the most recent item.
    /// </summary>
    public static IAsyncEnumerable<Deferred<T>> Latest<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        return source.ToObservable().Latest();
    }
}
