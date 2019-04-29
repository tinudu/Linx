namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns distinct elements from a sequence
        /// </summary>
        public static IAsyncEnumerableObs<T> DistinctUntilChanged<T>(this IAsyncEnumerableObs<T> source, IEqualityComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    if (!await ae.MoveNextAsync()) return;
                    var prev = ae.Current;
                    await yield(prev);

                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (comparer.Equals(current, prev)) continue;
                        prev = current;
                        await yield(prev);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
