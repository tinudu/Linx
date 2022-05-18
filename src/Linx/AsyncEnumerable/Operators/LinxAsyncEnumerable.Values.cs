using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets the values of the items that have a value.
    /// </summary>
    public static IAsyncEnumerable<T> Values<T>(this IAsyncEnumerable<T?> source) where T : struct
        => source.Where(x => x.HasValue).Select(x => x.GetValueOrDefault());
}
