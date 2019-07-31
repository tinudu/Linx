namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns the minimum non-null element of a sequence, if any.
        /// </summary>
        public static Maybe<T> MinMaybe<T>(this IEnumerable<T> source, IComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            using (var e = source.Where(x => x != null).GetEnumerator())
            {
                if (!e.MoveNext()) return default;
                var min = e.Current;

                while (e.MoveNext())
                {
                    var current = e.Current;
                    if (comparer.Compare(current, min) < 0) min = current;
                }

                return min;
            }
        }

        /// <summary>
        /// Returns the minimum non-null element of a projection of a sequence, if any.
        /// </summary>
        public static Maybe<TResult> MinMaybe<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, IComparer<TResult> comparer = null)
            => source.Select(selector).MinMaybe(comparer);

        /// <summary>
        /// Returns the elements of a sequence witch have the minimum non-null key.
        /// </summary>
        public static IList<TSource> MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, Maybe<TKey>> maybeKeySelector, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maybeKeySelector == null) throw new ArgumentNullException(nameof(maybeKeySelector));
            if (comparer == null) comparer = Comparer<TKey>.Default;

            TKey min = default;
            var result = new List<TSource>();
            foreach (var element in source)
            {
                var maybeKey = maybeKeySelector(element);
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
                result.Add(element);
            }
            return result;
        }

        /// <summary>
        /// Returns the maximum non-null element of a sequence, if any.
        /// </summary>
        public static Maybe<T> MaxMaybe<T>(this IEnumerable<T> source, IComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            using (var e = source.Where(x => x != null).GetEnumerator())
            {
                if (!e.MoveNext()) return default;
                var max = e.Current;

                while (e.MoveNext())
                {
                    var current = e.Current;
                    if (comparer.Compare(current, max) > 0) max = current;
                }

                return max;
            }
        }

        /// <summary>
        /// Returns the maximum non-null element of a projection of a sequence, if any.
        /// </summary>
        public static Maybe<TResult> MaxMaybe<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, IComparer<TResult> comparer = null)
            => source.Select(selector).MaxMaybe(comparer);

        /// <summary>
        /// Returns the elements of a sequence witch have the maximum non-null key.
        /// </summary>
        public static IList<TSource> MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, Maybe<TKey>> maybeKeySelector, IComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maybeKeySelector == null) throw new ArgumentNullException(nameof(maybeKeySelector));
            if (comparer == null) comparer = Comparer<TKey>.Default;

            TKey max = default;
            var result = new List<TSource>();
            foreach (var element in source)
            {
                var maybeKey = maybeKeySelector(element);
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
                result.Add(element);
            }
            return result;
        }

    }
}