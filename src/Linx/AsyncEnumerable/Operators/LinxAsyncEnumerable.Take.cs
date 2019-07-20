﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return Empty<T>();

            return Generate<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                var remaining = count;
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        if (!await yield(ae.Current).ConfigureAwait(false)) return;
                        if (--remaining == 0) break;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
