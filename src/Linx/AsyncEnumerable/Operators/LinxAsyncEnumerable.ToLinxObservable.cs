namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Observable;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Convert a <see cref="IEnumerable{T}"/> to a <see cref="ILinxObservable{T}"/>.
        /// </summary>
        public static ILinxObservable<T> ToLinxObservable<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return source is ILinxObservable<T> lo
                ? lo
                : new AnonymousAsyncEnumerable<T>(source.GetAsyncEnumerator, source.ToString());
        }
    }
}
