using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Produces the set intersection of two sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Intersect<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, IEqualityComparer<T>? comparer = null)
    {
        if (first is null) throw new ArgumentNullException(nameof(first));
        if (second is null) throw new ArgumentNullException(nameof(second));
        if (comparer is null) comparer = EqualityComparer<T>.Default;

        var seq = first.Select(x => (x, true)).Merge(second.Select(x => (x, false)));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var set1 = new HashSet<T>(comparer);
            var set2 = new HashSet<T>(comparer);

            await foreach (var (x, b) in seq.WithCancellation(token).ConfigureAwait(false))
                if (b ? set1.Add(x) && set2.Contains(x) : set2.Add(x) && set1.Contains(x))
                    yield return x;
        }
    }
}
