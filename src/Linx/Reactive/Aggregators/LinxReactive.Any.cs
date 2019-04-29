﻿namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        public static async Task<bool> Any<T>(this IAsyncEnumerableObs<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try { return await ae.MoveNextAsync(); }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        public static async Task<bool> Any<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).Any(token).ConfigureAwait(false);
    }
}
