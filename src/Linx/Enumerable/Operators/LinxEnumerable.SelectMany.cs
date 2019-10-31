namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using AsyncEnumerable;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TResult>> collectionSelector,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));

            return source
                .Select(collectionSelector)
                .Merge(maxConcurrent)
                .WithName();
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, int, IAsyncEnumerable<TResult>> collectionSelector,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));

            return source
                .Select(collectionSelector)
                .Merge(maxConcurrent)
                .WithName();
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source
                .Select(s => collectionSelector(s).Select(c => (s, c)))
                .Merge(maxConcurrent)
                .Select(t => resultSelector(t.s, t.c))
                .WithName();
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, int, IAsyncEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return source
                .Select((s, i) => collectionSelector(s, i).Select(c => (s, c)))
                .Merge(maxConcurrent)
                .Select(t => resultSelector(t.s, t.c))
                .WithName();
        }
    }
}
