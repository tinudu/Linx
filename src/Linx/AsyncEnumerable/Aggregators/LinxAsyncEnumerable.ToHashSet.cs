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

            var result = new HashSet<T>(comparer);
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                result.Add(item);
            return result;
        }
    }
}
