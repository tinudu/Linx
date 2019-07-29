namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Returns the last element of a sequence, if any.
        /// </summary>
        public static Maybe<T> LastMaybe<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default;
                var last = e.Current;
                while (e.MoveNext()) last = e.Current;
                return last;
            }
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition, if any.
        /// </summary>
        public static Maybe<T> LastOrDefault<T>(this IEnumerable<T> source, Func<T, bool> predicate)
            => source.Where(predicate).LastMaybe();
    }
}
