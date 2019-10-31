namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Aggregate elements into a list.
        /// </summary>
        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var result = new List<T>();
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                result.Add(item);
            return result;
        }
    }
}