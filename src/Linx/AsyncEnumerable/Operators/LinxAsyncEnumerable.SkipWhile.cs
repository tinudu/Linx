namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Skip items while the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> SkipWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    if (!await ae.MoveNextAsync()) return;

                    T current;
                    while (true)
                    {
                        current = ae.Current;
                        if (!predicate(current)) break;
                        if (!await ae.MoveNextAsync()) return;
                    }

                    while (true)
                    {
                        await yield(current).ConfigureAwait(false);
                        if (!await ae.MoveNextAsync()) return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
