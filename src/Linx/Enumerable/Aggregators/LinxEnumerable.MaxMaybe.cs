namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns a <see cref="Maybe{T}"/> containing the maximum element, if any.
        /// </summary>
        public static Maybe<T> MaxMaybe<T>(this IEnumerable<T> source, IComparer<T> comparer = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = Comparer<T>.Default;

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return new Maybe<T>();
                var max = e.Current;
                while (e.MoveNext())
                {
                    var current = e.Current;
                    if (comparer.Compare(current, max) > 0)
                        max = current;
                }
                return new Maybe<T>(max);
            }
        }
    }
}
