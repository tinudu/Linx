namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Copies the elements of <paramref name="source"/> to the <paramref name="yield"/>.
        /// </summary>
        /// <returns>Whether <paramref name="yield"/> accepts more elements.</returns>
        public static async Task<bool> CopyTo<T>(this IAsyncEnumerable<T> source, YieldAsyncDelegate<T> yield, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (yield == null) throw new ArgumentNullException(nameof(yield));
            token.ThrowIfCancellationRequested();

            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                if (!await yield(item).ConfigureAwait(false))
                    return false;
            return true;
        }
    }
}
