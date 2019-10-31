namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Produces the set intersection of two sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Intersect<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, IEqualityComparer<T> comparer = null)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            var seq = first.Select(x => (x, true)).Merge(second.Select(x => (x, false)));
            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var set1 = new HashSet<T>(comparer);
                var set2 = new HashSet<T>(comparer);

                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var (x, b) in seq.WithCancellation(token).ConfigureAwait(false))
                {
                    if (b ? !set1.Add(x) || !set2.Contains(x) : !set2.Add(x) || !set1.Contains(x)) continue;
                    yield return x;
                }
            }
        }
    }
}
