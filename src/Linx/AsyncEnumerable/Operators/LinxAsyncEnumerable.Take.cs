namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return Empty<T>();

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var remaining = count;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                {
                    yield return item;
                    if (--remaining == 0)
                        break;
                }
            }
        }
    }
}
