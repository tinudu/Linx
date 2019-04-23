namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Consumes <paramref name="source"/> ignoring its elements.
        /// </summary>
        public static async Task IgnoreElements<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try { while (await ae.MoveNextAsync()) { } }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
