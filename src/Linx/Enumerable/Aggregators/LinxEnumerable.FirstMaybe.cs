namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns a <see cref="Maybe{T}"/> containing the first element, if any.
        /// </summary>
        public static Maybe<T> FirstMaybe<T>(IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using (var e = source.GetEnumerator())
                return e.MoveNext() ? new Maybe<T>(e.Current) : default;
        }
    }
}
