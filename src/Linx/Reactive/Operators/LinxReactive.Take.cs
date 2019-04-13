namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return Empty<T>();

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                var remaining = count;
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        await yield(ae.Current);
                        if (--remaining == 0) break;
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
