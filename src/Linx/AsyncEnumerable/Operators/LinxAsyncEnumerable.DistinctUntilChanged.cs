namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns distinct elements from a sequence
        /// </summary>
        public static IAsyncEnumerable<T> DistinctUntilChanged<T>(this IAsyncEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            return Create<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    if (!await ae.MoveNextAsync()) return;
                    var prev = ae.Current;
                    if (!await yield(prev).ConfigureAwait(false)) return;

                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (comparer.Equals(current, prev)) continue;
                        prev = current;
                        if (!await yield(prev).ConfigureAwait(false)) return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
