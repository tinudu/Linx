namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns the element at a specified index in a sequence, if any.
        /// </summary>
        public static Maybe<T> ElementAtMaybe<T>(this IEnumerable<T> source, int index)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (index < 0) return default;

            // ReSharper disable once GenericEnumeratorNotDisposed
            using var e = source.GetEnumerator();
            var i = 0;
            while (e.MoveNext())
            {
                if (i == index) return e.Current;
                i++;
            }

            return default;
        }
    }
}
