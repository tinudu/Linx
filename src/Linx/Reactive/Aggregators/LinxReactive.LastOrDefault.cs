﻿namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the last element of a sequence, or a default value if the sequence contains no elements.
        /// </summary>
        public static async Task<T> LastOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var last = ae.Current;
                while (await ae.MoveNextAsync()) last = ae.Current;
                return last;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition or a default value if no such element is found.
        /// </summary>
        public static async Task<T> LastOrDefault<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).LastOrDefault(token).ConfigureAwait(false);
    }
}
