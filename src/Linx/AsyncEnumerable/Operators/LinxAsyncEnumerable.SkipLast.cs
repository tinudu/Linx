namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Skips the last <paramref name="count"/> items.
        /// </summary>
        public static IAsyncEnumerable<T> SkipLast<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return source;

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var q = new Queue<T>(count);
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                {
                    if (q.Count == count)
                        yield return q.Dequeue();
                    q.Enqueue(item);
                }
            }
        }
    }
}
