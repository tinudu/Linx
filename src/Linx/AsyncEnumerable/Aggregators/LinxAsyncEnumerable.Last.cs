﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the last element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static async Task<T> Last<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsNoElement);
                var last = ae.Current;
                while (await ae.MoveNextAsync()) last = ae.Current;
                return last;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a specified condition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static async Task<T> Last<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).Last(token).ConfigureAwait(false);
    }
}