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
        public static async Task CopyTo<T>(this IAsyncEnumerable<T> source, AcceptorDelegate<T> yield, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (yield == null) throw new ArgumentNullException(nameof(yield));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    await yield(ae.Current).ConfigureAwait(false);
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
