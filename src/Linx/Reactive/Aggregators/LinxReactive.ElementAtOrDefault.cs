namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        public static async Task<T> ElementAtOrDefault<T>(this IAsyncEnumerableObs<T> source, int index, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (index < 0) return default;

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var i = 0;
                while (await ae.MoveNextAsync())
                    if (i++ == index)
                        return ae.Current;

                return default;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
