namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns a <see cref="Maybe{T}"/> containing the single element, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
        public static Maybe<T> SingleMaybe<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return new Maybe<T>();
                var result = new Maybe<T>(e.Current);
                if (!e.MoveNext()) return result;
                throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
            }
        }
    }
}
