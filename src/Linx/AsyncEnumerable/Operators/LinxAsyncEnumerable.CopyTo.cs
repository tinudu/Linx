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
        public static async Task<bool> CopyTo<T>(this IAsyncEnumerable<T> source, YieldDelegate<T> yield, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (yield == null) throw new ArgumentNullException(nameof(yield));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    if (!await yield(ae.Current).ConfigureAwait(false))
                        return false;
                return true;
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
