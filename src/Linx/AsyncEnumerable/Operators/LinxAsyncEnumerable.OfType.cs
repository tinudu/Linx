namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Filters the elements of an observable sequence based on the specified type.
        /// </summary>
        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync() && ae.Current is TResult next)
                        await yield(next).ConfigureAwait(false);
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Filters the elements of an observable sequence based on the specified type.
        /// </summary>
        public static IAsyncEnumerable<TResult> OfType<TResult>(this IAsyncEnumerable<object> source, TResult sample) => source.OfType<TResult>();

    }
}
