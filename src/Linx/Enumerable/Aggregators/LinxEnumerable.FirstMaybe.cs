namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns the first element of a sequence, if any.
        /// </summary>
        public static Maybe<T> FirstMaybe<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // ReSharper disable once GenericEnumeratorNotDisposed
            using var e = source.GetEnumerator();
            return e.MoveNext() ? e.Current : default;
        }

        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition, if any.
        /// </summary>
        public static Maybe<T> FirstMaybe<T>(this IEnumerable<T> source, Func<T, bool> predicate)
            => source.Where(predicate).FirstMaybe();
    }
}
