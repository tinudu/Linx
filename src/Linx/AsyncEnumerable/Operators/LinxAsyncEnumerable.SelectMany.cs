namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        #region async/sync

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IEnumerable<TResult>> collectionSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            return Iterator();

            async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                await foreach (var inner in source.WithCancellation(token).ConfigureAwait(false))
                    foreach (var element in collectionSelector(inner))
                        yield return element;
            }
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
            return Iterator();

            async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var i = 0;
                await foreach (var inner in source.WithCancellation(token).ConfigureAwait(false))
                    foreach (var element in collectionSelector(inner, unchecked(i++)))
                        yield return element;
            }
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
            return Iterator();

            async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                await foreach (var inner in source.WithCancellation(token).ConfigureAwait(false))
                    foreach (var element in collectionSelector(inner))
                        yield return resultSelector(inner, element);
            }
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
            return Iterator();

            async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var i = 0;
                await foreach (var inner in source.WithCancellation(token).ConfigureAwait(false))
                    foreach (var element in collectionSelector(inner, unchecked(i++)))
                        yield return resultSelector(inner, element);
            }
        }

        #endregion

        #region async/async

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, IAsyncEnumerable<TResult>> collectionSelector,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));

            return source
                .Select(collectionSelector)
                .Merge(maxConcurrent);
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/> and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TResult>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, int, IAsyncEnumerable<TResult>> collectionSelector,
            int maxConcurrent = int.MaxValue)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));

            return source
                .Select(collectionSelector)
                .Merge(maxConcurrent);
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
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
                .Select(t => resultSelector(t.s, t.c));
        }

        /// <summary>
        /// Projects each element of a sequence to an <see cref="IAsyncEnumerable{T}"/>, flattens the resulting sequences into one sequence, and invokes a result selector function on each element therein.
        /// </summary>
        public static IAsyncEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this IAsyncEnumerable<TSource> source,
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
                .Select(t => resultSelector(t.s, t.c));
        }

        #endregion

        #region sync/async

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
                .Merge(maxConcurrent);
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
                .Merge(maxConcurrent);
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
                .Select(t => resultSelector(t.s, t.c));
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
                .Select(t => resultSelector(t.s, t.c));
        }

        #endregion
    }
}