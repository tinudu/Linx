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
        /// Invokes a transform function on each element of a sequence and returns the minimum non-null element.
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
        /// Invokes a transform function on each element of a sequence and returns the minimum element, if any.
        /// </summary>
        public static async Task<Maybe<TResult>> MinMaybe<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult> comparer = null)
            => await source.Select(selector).MinMaybe(token, comparer).ConfigureAwait(false);

        /// <summary>
        /// Returns the elements in <paramref name="source"/> with the minimum key value.
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
        /// Returns the elements in <paramref name="source"/> with the minimum key value that matches a condition.
        /// </summary>
        public static async Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, bool> predicate, CancellationToken token, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
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
                    if(!predicate(key)) continue;
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
        /// Returns the maximum non-null element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
        public static async Task<T> Max<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T> comparer = null)
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
        /// Invokes a transform function on each element of a sequence and returns the maximum non-null element.
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
        /// Invokes a transform function on each element of a sequence and returns the maximum element, if any.
        /// </summary>
        public static async Task<Maybe<TResult>> MaxMaybe<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult> comparer = null)
            => await source.Select(selector).MaxMaybe(token, comparer).ConfigureAwait(false);

        /// <summary>
        /// Returns the elements in <paramref name="source"/> with the maximum key value.
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
        /// Returns the elements in <paramref name="source"/> with the maximum key value that matches a condition.
        /// </summary>
        public static async Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, bool> predicate, CancellationToken token, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
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
                    if(!predicate(key)) continue;
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

    }
}