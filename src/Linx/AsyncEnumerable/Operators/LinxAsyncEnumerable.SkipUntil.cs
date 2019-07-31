namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Skip items until the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> SkipUntil<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    if (!await ae.MoveNextAsync()) return;
                    var current = ae.Current;

                    while (true)
                    {
                        if (predicate(current)) break;
                        if (!await ae.MoveNextAsync()) return;
                        current = ae.Current;
                    }

                    while (true)
                    {
                        if (!await yield(current).ConfigureAwait(false)) return;
                        if (!await ae.MoveNextAsync()) return;
                        current = ae.Current;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
