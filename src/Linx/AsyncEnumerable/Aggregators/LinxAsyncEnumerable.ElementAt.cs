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
        public static async Task<T> ElementAt<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (index < 0) throw new IndexOutOfRangeException();
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var i = 0;
                while (await ae.MoveNextAsync())
                    if (i++ == index)
                        return ae.Current;

                throw new IndexOutOfRangeException();
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
