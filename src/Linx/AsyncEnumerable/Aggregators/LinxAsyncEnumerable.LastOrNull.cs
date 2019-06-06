﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the last element of a sequence, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<T?> LastOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var last = ae.Current;
                while (await ae.MoveNextAsync()) last = ae.Current;
                return last;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition or null if no such element is found.
        /// </summary>
        public static async Task<T?> LastOrNull<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
            => await source.Where(predicate).LastOrNull(token).ConfigureAwait(false);
    }
}
