using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Produces the set union of two sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Union<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, IEqualityComparer<T>? comparer = null)
        => first.Merge(second).Distinct(comparer);
}
