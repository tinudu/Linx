namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns the single element of a sequence, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
        public static Maybe<T> SingleMaybe<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // ReSharper disable once GenericEnumeratorNotDisposed
            using var e = source.GetEnumerator();
            if (!e.MoveNext()) return new Maybe<T>();
            var single = e.Current;
            if (!e.MoveNext()) return new Maybe<T>(single);
            throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
        }

        /// <summary>
        /// Returns the single element of a sequence that satisfies a condition, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
        public static Maybe<T> SingleMaybe<T>(this IEnumerable<T> source, Func<T, bool> predicate)
            => source.Where(predicate).SingleMaybe();
    }
}
