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

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return selector(item);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return selector(item, i++);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return await selector(item, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return await selector(item, i++, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                    yield return await selector(item, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                    yield return await selector(item, i++, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into the corresponding element of another sequence.
        /// </summary>
        public static IAsyncEnumerable<T2> Select<T1, T2>(this IAsyncEnumerable<T1> first, IAsyncEnumerable<T2> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));

            return Create(GetEnumerator);

            async IAsyncEnumerator<T2> GetEnumerator(CancellationToken token)
            {
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = first.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = second.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                while (await ae1.MoveNextAsync() && await ae2.MoveNextAsync())
                    yield return ae2.Current;
            }
        }
    }
}
