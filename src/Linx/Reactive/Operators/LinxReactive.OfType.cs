namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Filters the elements of an observable sequence based on the specified type.
        /// </summary>
        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<TResult>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync() && ae.Current is TResult next)
                        await yield(next);
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }

        /// <summary>
        /// Filters the elements of an observable sequence based on the specified type.
        /// </summary>
        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source, TResult sample) => source.OfType<TResult>();

    }
}
