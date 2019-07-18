namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
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
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            var seq = first.Select(x => (x, true)).Merge(second.Select(x => (x, false)));

            return Produce<T>(async (yield, token) =>
            {
                var set1 = new HashSet<T>(comparer);
                var set2 = new HashSet<T>(comparer);

                var ae = seq.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var (x, b) = ae.Current;
                        if (b ? !set1.Add(x) || !set2.Contains(x) : !set2.Add(x) || !set1.Contains(x)) continue;
                        if (!await yield(x).ConfigureAwait(false)) return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
