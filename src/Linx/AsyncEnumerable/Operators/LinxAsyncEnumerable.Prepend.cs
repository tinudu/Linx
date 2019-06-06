﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Prepends a value to the end of the sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Prepend<T>(this IAsyncEnumerable<T> source, T element)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                await yield(element).ConfigureAwait(false);

                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(ae.Current).ConfigureAwait(false);
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}