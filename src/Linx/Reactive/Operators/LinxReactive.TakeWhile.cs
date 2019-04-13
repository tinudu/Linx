namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns values from an observable sequence as long as a specified condition is true, and then skips the remaining values.
        /// </summary>
        public static IAsyncEnumerable<T> TakeWhile<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (true)
                    {
                        if (!await ae.MoveNextAsync()) return;
                        var current = ae.Current;
                        if (!predicate(current)) return;
                        await yield(current);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }

        /// <summary>
        /// Returns values from an observable sequence as long as a specified condition is true, and then skips the remaining values.
        /// </summary>
        public static IAsyncEnumerable<T> TakeWhile<T>(this IAsyncEnumerable<T> source, Func<T, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    var i = 0;
                    while (true)
                    {
                        if (!await ae.MoveNextAsync()) return;
                        var current = ae.Current;
                        if (!predicate(current, i++)) return;
                        await yield(current);
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
