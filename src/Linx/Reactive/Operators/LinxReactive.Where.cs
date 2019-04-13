namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (predicate(current))
                            await yield(current);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, CancellationToken, Task<bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        if (await predicate(current, token).ConfigureAwait(false))
                            await yield(current);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
