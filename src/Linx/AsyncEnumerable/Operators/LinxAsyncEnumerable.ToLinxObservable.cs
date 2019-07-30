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
            switch (source)
            {
                case ILinxObservable<T> lo:
                    return lo;
                case null:
                    throw new ArgumentNullException(nameof(source));
                default:
                    return new AnonymousAsyncEnumerable<T>(source.GetAsyncEnumerator, source.ToString());
            }
        }
    }
}
