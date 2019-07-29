namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns a <see cref="Maybe{T}"/> containing the minimum element, if any.
        /// </summary>
        public static Maybe<T> MinMaybe<T>(this IEnumerable<T> source, IComparer<T> comparer = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return new Maybe<T>();
                var min = e.Current;
                while (e.MoveNext())
                {
                    var current = e.Current;
                    if (comparer.Compare(current, min) < 0)
                        min = current;
                }
                return new Maybe<T>(min);
            }
        }
    }
}
