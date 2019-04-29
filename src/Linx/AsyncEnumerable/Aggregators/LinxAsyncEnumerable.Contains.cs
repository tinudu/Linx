namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Determines whether a sequence contains a specified element.
        /// </summary>
        public static async Task<bool> Contains<T>(this IAsyncEnumerable<T> source, T value, CancellationToken token, IEqualityComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await ae.MoveNextAsync())
                    if (comparer.Equals(ae.Current, value))
                        return true;
                return false;
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
