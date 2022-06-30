using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets the most recent item.
    /// </summary>
    public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source) => throw new NotImplementedException();
}
