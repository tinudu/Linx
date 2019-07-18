namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns all non-null values from <paramref name="source"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Values<T>(this IAsyncEnumerable<T?> source) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (!current.HasValue) continue;
                        if (!await yield(current.GetValueOrDefault()).ConfigureAwait(false)) return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
