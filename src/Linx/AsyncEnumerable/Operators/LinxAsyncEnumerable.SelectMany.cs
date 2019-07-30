﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TResult>> collectionSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                        foreach (var r in collectionSelector(ae.Current))
                            if (!await yield(r).ConfigureAwait(false))
                                return;
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IEnumerable<TResult>> collectionSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var i = 0;
                    while (await ae.MoveNextAsync())
                        foreach (var r in collectionSelector(ae.Current, i++))
                            if (!await yield(r).ConfigureAwait(false))
                                return;
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        foreach (var r in collectionSelector(current))
                            if (!await yield(resultSelector(current, r)).ConfigureAwait(false))
                                return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create<TResult>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    var i = 0;
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        foreach (var r in collectionSelector(current, i++))
                            if (!await yield(resultSelector(current, r)).ConfigureAwait(false))
                                return;
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
            => source.Select(s => collectionSelector(s).Select(c => resultSelector(s, c))).Merge();

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
            => source.Select((s, i) => collectionSelector(s, i).Select(c => resultSelector(s, c))).Merge();
    }
}