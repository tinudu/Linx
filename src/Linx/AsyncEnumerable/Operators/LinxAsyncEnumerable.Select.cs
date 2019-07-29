namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync() && await yield(selector(ae.Current)).ConfigureAwait(false)) { }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var i = 0;
                    while (await ae.MoveNextAsync() && await yield(selector(ae.Current, i++)).ConfigureAwait(false)) { }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync() && await yield(await selector(ae.Current, token).ConfigureAwait(false))) { }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var i = 0;
                    while (await ae.MoveNextAsync() && await yield(await selector(ae.Current, i++, token).ConfigureAwait(false)).ConfigureAwait(false)) { }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
