namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
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

            return Produce<T>(async (yield, token) =>
            {
                var q = new Queue<T>(count);
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        if (q.Count == count)
                            await yield(q.Dequeue()).ConfigureAwait(false);
                        q.Enqueue(ae.Current);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
