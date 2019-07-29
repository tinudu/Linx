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
        /// Invokes a transform function on each element of a sequence and returns the minimum element, if any.
        /// </summary>
        public static Maybe<TResult> MinMaybe<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, IComparer<TResult> comparer = null)
            => source.Select(selector).MinMaybe(comparer);

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
        /// Invokes a transform function on each element of a sequence and returns the maximum element, if any.
        /// </summary>
        public static Maybe<TResult> MaxMaybe<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector, IComparer<TResult> comparer = null)
            => source.Select(selector).MaxMaybe(comparer);

    }
}