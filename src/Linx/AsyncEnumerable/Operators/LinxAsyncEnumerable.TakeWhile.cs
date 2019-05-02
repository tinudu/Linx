﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns values while the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> TakeWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (true)
                    {
                        if (!await ae.MoveNextAsync()) return;
                        var current = ae.Current;
                        if (!predicate(current)) return;
                        await yield(current);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Returns values while the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> TakeWhile<T>(this IAsyncEnumerable<T> source, Func<T, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var i = 0;
                    while (true)
                    {
                        if (!await ae.MoveNextAsync()) return;
                        var current = ae.Current;
                        if (!predicate(current, i++)) return;
                        await yield(current);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}