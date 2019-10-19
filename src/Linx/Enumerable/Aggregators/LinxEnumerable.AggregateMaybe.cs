namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Applies an accumulator function over a sequence.
        /// </summary>
        public static Maybe<T> AggregateMaybe<T>(
            this IEnumerable<T> source,
            Func<T, T, T> accumulator)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));

            // ReSharper disable once GenericEnumeratorNotDisposed
            using var e = source.GetEnumerator();
            if (!e.MoveNext()) return default;
            var seed = e.Current;
            while (e.MoveNext())
                seed = accumulator(seed, e.Current);
            return seed;
        }
    }
}
