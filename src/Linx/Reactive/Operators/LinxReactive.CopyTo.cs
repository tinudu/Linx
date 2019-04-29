namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Copies the elements of <paramref name="source"/> to the <paramref name="acceptor"/>.
        /// </summary>
        public static async Task CopyTo<T>(this IAsyncEnumerableObs<T> source, AcceptorDelegate<T> acceptor, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (acceptor == null) throw new ArgumentNullException(nameof(acceptor));
            token.ThrowIfCancellationRequested();

            var ae = source.GetAsyncEnumerator(token);
            try
            {
                while (await ae.MoveNextAsync())
                    await acceptor(ae.Current);
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
