namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Skip the first <paramref name="count"/> items.
        /// </summary>
        public static IAsyncEnumerableObs<T> Skip<T>(this IAsyncEnumerableObs<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return source;

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    var skip = count;
                    while (await ae.MoveNextAsync())
                    {
                        if (skip > 0) skip--;
                        else await yield(ae.Current);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
