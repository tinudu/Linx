﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        public static async Task<T?> ElementAtOrNull<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token) where T: struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (index < 0) return default;
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var i = 0;
                while (await ae.MoveNextAsync())
                    if (i++ == index)
                        return ae.Current;

                return default;
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}