namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerableObs<TResult> Select<TSource, TResult>(this IAsyncEnumerableObs<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Produce<TResult>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(selector(ae.Current));
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerableObs<TResult> Select<TSource, TResult>(this IAsyncEnumerableObs<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Produce<TResult>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(await selector(ae.Current, token).ConfigureAwait(false));
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
