namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Creates a <see cref="HashSet{T}"/> from a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static async Task<HashSet<T>> ToHashSet<T>(this IAsyncEnumerable<T> source, CancellationToken token, IEqualityComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var result = new HashSet<T>(comparer);
                while (await ae.MoveNextAsync())
                    result.Add(ae.Current);
                return result;
            }
            finally { await ae.DisposeAsync(); }
        }
    }
}
