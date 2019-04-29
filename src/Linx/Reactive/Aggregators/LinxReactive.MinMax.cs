namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the minimum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Min<T>(this IAsyncEnumerable<T> source, IComparer<T> comparer, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.Where(x => x != null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsNoElement);
                var min = ae.Current;

                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if (comparer.Compare(current, min) < 0) min = current;
                }

                return min;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the minimum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Min<T>(this IAsyncEnumerable<T> source, CancellationToken token)
            => await source.Min(null, token).ConfigureAwait(false);

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the minimum non-null element.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<TResult> Min<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token)
            => await source.Select(selector).Min(null, token).ConfigureAwait(false);

        /// <summary>
        /// Returns the minimum non-null element of a sequence, or a default value if the sequence contains no non-null elements.
        /// </summary>
        public static async Task<T> MinOrDefault<T>(this IAsyncEnumerable<T> source, IComparer<T> comparer, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.Where(x => x != null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var min = ae.Current;

                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if (comparer.Compare(current, min) < 0) min = current;
                }

                return min;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the minimum non-null element of a sequence, or a default value if the sequence contains no non-null elements.
        /// </summary>
        public static async Task<T> MinOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token)
            => await source.MinOrDefault(null, token).ConfigureAwait(false);

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the minimum element, or a default value if the sequence contains no non-null elements.
        /// </summary>
        public static async Task<TResult> MinOrDefault<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token)
            => await source.Select(selector).MinOrDefault(null, token).ConfigureAwait(false);

        /// <summary>
        /// Returns the minimum element of a sequence, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<T?> MinOrNull<T>(this IAsyncEnumerable<T> source, IComparer<T> comparer, CancellationToken token) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var min = ae.Current;

                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if (comparer.Compare(current, min) < 0) min = current;
                }

                return min;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the minimum element of a sequence, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<T?> MinOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
            => await source.MinOrNull(null, token).ConfigureAwait(false);

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the minimum element, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<TResult?> MinOrNull<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token) where TResult : struct
            => await source.Select(selector).MinOrNull(null, token).ConfigureAwait(false);

        /// <summary>
        /// Returns the maximum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Max<T>(this IAsyncEnumerable<T> source, IComparer<T> comparer, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.Where(x => x != null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsNoElement);
                var max = ae.Current;

                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if (comparer.Compare(current, max) > 0) max = current;
                }

                return max;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the maximum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Max<T>(this IAsyncEnumerable<T> source, CancellationToken token)
            => await source.Max(null, token).ConfigureAwait(false);

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the maximum non-null element.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<TResult> Max<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token)
            => await source.Select(selector).Max(null, token).ConfigureAwait(false);

        /// <summary>
        /// Returns the maximum non-null element of a sequence, or a default value if the sequence contains no non-null elements.
        /// </summary>
        public static async Task<T> MaxOrDefault<T>(this IAsyncEnumerable<T> source, IComparer<T> comparer, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.Where(x => x != null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var max = ae.Current;

                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if (comparer.Compare(current, max) > 0) max = current;
                }

                return max;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the maximum non-null element of a sequence, or a default value if the sequence contains no non-null elements.
        /// </summary>
        public static async Task<T> MaxOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token)
            => await source.MaxOrDefault(null, token).ConfigureAwait(false);

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the maximum element, or a default value if the sequence contains no non-null elements.
        /// </summary>
        public static async Task<TResult> MaxOrDefault<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token)
            => await source.Select(selector).MaxOrDefault(null, token).ConfigureAwait(false);

        /// <summary>
        /// Returns the maximum element of a sequence, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<T?> MaxOrNull<T>(this IAsyncEnumerable<T> source, IComparer<T> comparer, CancellationToken token) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            token.ThrowIfCancellationRequested();
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var max = ae.Current;

                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if (comparer.Compare(current, max) > 0) max = current;
                }

                return max;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the maximum element of a sequence, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<T?> MaxOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
            => await source.MaxOrNull(null, token).ConfigureAwait(false);

        /// <summary>
        /// Invokes a transform function on each element of a sequence and returns the maximum element, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<TResult?> MaxOrNull<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token) where TResult : struct
            => await source.Select(selector).MaxOrNull(null, token).ConfigureAwait(false);

    }
}