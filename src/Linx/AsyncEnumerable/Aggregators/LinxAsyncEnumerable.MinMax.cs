namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the minimum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Min<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T> comparer = null)
        {
            var maybe = await source.MinMaybe(token, comparer);
            return maybe.HasValue ? maybe.GetValueOrDefault() : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
        }

        /// <summary>
        /// Returns the minimum non-null element of a projection of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<TResult> Min<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult> comparer = null)
            => await source.Select(selector).Min(token, comparer).ConfigureAwait(false);

        /// <summary>
        /// Returns the minimum non-null element of a sequence, if any.
        /// </summary>
        public static async Task<Maybe<T>> MinMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T> comparer = null)
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
        /// Returns the minimum non-null element of a projection of a sequence, if any.
        /// </summary>
        public static async Task<Maybe<TResult>> MinMaybe<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult> comparer = null)
            => await source.Select(selector).MinMaybe(token, comparer).ConfigureAwait(false);

        /// <summary>
        /// Returns the elements of a sequence with the minimum non-null key.
        /// </summary>
        public static async Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken token, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (comparer == null) comparer = Comparer<TKey>.Default;
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                TKey min = default;
                var result = new List<TSource>();
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    var key = keySelector(current);
                    if (key == null) continue;
                    if (result.Count == 0)
                    {
                        min = key;
                        result.Add(current);
                    }
                    else
                    {
                        var cmp = comparer.Compare(key, min);
                        if (cmp > 0) continue;
                        min = key;
                        if (cmp < 0) result.Clear();
                        result.Add(current);
                    }
                }
                return result;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the elements of a sequence witch have the minimum non-null key.
        /// </summary>
        public static async Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, Maybe<TKey>> maybeKeySelector, CancellationToken token, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maybeKeySelector == null) throw new ArgumentNullException(nameof(maybeKeySelector));
            if (comparer == null) comparer = Comparer<TKey>.Default;
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                TKey min = default;
                var result = new List<TSource>();
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    var maybeKey = maybeKeySelector(current);
                    if (!maybeKey.HasValue) continue;
                    var key = maybeKey.GetValueOrDefault();
                    if (key == null) continue;
                    if (result.Count == 0)
                        min = key;
                    else
                    {
                        var cmp = comparer.Compare(key, min);
                        if (cmp > 0) continue;
                        min = key;
                        if (cmp < 0) result.Clear();
                    }
                    result.Add(current);
                }
                return result;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the maximum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Max<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T> comparer = null)
        {
            var maybe = await source.MaxMaybe(token, comparer);
            return maybe.HasValue ? maybe.GetValueOrDefault() : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
        }

        /// <summary>
        /// Returns the maximum non-null element of a projection of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<TResult> Max<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult> comparer = null)
            => await source.Select(selector).Max(token, comparer).ConfigureAwait(false);

        /// <summary>
        /// Returns the maximum non-null element of a sequence, if any.
        /// </summary>
        public static async Task<Maybe<T>> MaxMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T> comparer = null)
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
        /// Returns the maximum non-null element of a projection of a sequence, if any.
        /// </summary>
        public static async Task<Maybe<TResult>> MaxMaybe<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult> comparer = null)
            => await source.Select(selector).MaxMaybe(token, comparer).ConfigureAwait(false);

        /// <summary>
        /// Returns the elements of a sequence with the maximum non-null key.
        /// </summary>
        public static async Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken token, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (comparer == null) comparer = Comparer<TKey>.Default;
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                TKey max = default;
                var result = new List<TSource>();
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    var key = keySelector(current);
                    if (key == null) continue;
                    if (result.Count == 0)
                    {
                        max = key;
                        result.Add(current);
                    }
                    else
                    {
                        var cmp = comparer.Compare(key, max);
                        if (cmp < 0) continue;
                        max = key;
                        if (cmp > 0) result.Clear();
                        result.Add(current);
                    }
                }
                return result;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the elements of a sequence witch have the maximum non-null key.
        /// </summary>
        public static async Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, Maybe<TKey>> maybeKeySelector, CancellationToken token, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maybeKeySelector == null) throw new ArgumentNullException(nameof(maybeKeySelector));
            if (comparer == null) comparer = Comparer<TKey>.Default;
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                TKey max = default;
                var result = new List<TSource>();
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    var maybeKey = maybeKeySelector(current);
                    if (!maybeKey.HasValue) continue;
                    var key = maybeKey.GetValueOrDefault();
                    if (key == null) continue;
                    if (result.Count == 0)
                        max = key;
                    else
                    {
                        var cmp = comparer.Compare(key, max);
                        if (cmp < 0) continue;
                        max = key;
                        if (cmp > 0) result.Clear();
                    }
                    result.Add(current);
                }
                return result;
            }
            finally { await ae.DisposeAsync(); }
        }

    }
}