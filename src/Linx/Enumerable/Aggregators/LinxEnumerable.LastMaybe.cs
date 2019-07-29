namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns a <see cref="Maybe{T}"/> containing the last element, if any.
        /// </summary>
        public static Maybe<T> LastMaybe<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new Maybe<T>();
            foreach (var element in source)
                result = new Maybe<T>(element);
            return result;
        }
    }
}
