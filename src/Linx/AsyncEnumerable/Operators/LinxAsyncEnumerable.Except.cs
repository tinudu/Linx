namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Produces the set difference of two sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Except<T>(this IAsyncEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T> comparer = null)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            return Produce<T>(async (yield, token) =>
            {
                var excluded = second as ISet<T> ?? new HashSet<T>(second, comparer);
                var included = new HashSet<T>(comparer);
                var ae = first.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (!excluded.Contains(current) && included.Add(current))
                            await yield(current).ConfigureAwait(false);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
