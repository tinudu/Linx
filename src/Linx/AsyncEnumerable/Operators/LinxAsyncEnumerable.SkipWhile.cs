﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
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

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                while (true)
                {
                    if (!await ae.MoveNextAsync())
                        yield break;
                    var item = ae.Current;
                    if (predicate(item)) continue;
                    yield return item;
                    break;
                }

                while (await ae.MoveNextAsync())
                    yield return ae.Current;
            }
        }
    }
}
