namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Skip the first <paramref name="count"/> items.
        /// </summary>
        public static IAsyncEnumerable<T> Skip<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return source;

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var skip = count;
                    while (await ae.MoveNextAsync())
                    {
                        if (skip > 0) skip--;
                        else await yield(ae.Current).ConfigureAwait(false);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
