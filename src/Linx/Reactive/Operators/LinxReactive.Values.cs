namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns all non-null values from <paramref name="source"/>.
        /// </summary>
        public static IAsyncEnumerableObs<T> Values<T>(this IAsyncEnumerableObs<T?> source) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (!current.HasValue) continue;
                        await yield(current.GetValueOrDefault());
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
