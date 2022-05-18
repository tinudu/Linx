using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns distinct elements from a sequence
    /// </summary>
    public static IAsyncEnumerable<T> DistinctUntilChanged<T>(this IAsyncEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (comparer == null) comparer = EqualityComparer<T>.Default;

        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            if (!await ae.MoveNextAsync())
                yield break;
            var prev = ae.Current;
            yield return prev;
            while (await ae.MoveNextAsync())
            {
                var current = ae.Current;
                if (comparer.Equals(current, prev))
                    continue;
                prev = current;
            }
        }
    }
}
