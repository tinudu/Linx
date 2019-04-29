namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Copies the elements of <paramref name="source"/> to the <paramref name="acceptor"/>.
        /// </summary>
        public static async Task CopyTo<T>(this IAsyncEnumerable<T> source, AcceptorDelegate<T> acceptor, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (acceptor == null) throw new ArgumentNullException(nameof(acceptor));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    await acceptor(ae.Current);
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
