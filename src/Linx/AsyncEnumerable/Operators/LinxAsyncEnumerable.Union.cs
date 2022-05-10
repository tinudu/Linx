namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Produces the set union of two sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Union<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, IEqualityComparer<T>? comparer = null)
            => first.Merge(second).Distinct(comparer);
    }
}
