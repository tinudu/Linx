namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns values until the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> TakeUntil<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        await yield(current);
                        if (predicate(current)) return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Returns values until the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> TakeUntil<T>(this IAsyncEnumerable<T> source, Func<T, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var i = 0;
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        await yield(current);
                        if (predicate(current, i++)) return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
