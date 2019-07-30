﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the elements of the specified sequence or the type parameter's default value in a singleton sequence if the sequence is empty.
        /// </summary>
        public static IAsyncEnumerable<T> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source) => source.DefaultIfEmpty(default);

        /// <summary>
        /// Returns the elements of the specified sequence or the specified <paramref name="default"/>, if the sequence is empty.
        /// </summary>
        public static IAsyncEnumerable<T> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source, T @default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    if (await ae.MoveNextAsync())
                        do { } while (await yield(ae.Current).ConfigureAwait(false) && await ae.MoveNextAsync());
                    else
                        await yield(@default).ConfigureAwait(false);
                }
                finally { await ae.DisposeAsync(); }
            }, source + ".DefaultIfEmpty");
        }
    }
}
