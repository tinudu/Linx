namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
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
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var q = new Queue<T>(count);
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
