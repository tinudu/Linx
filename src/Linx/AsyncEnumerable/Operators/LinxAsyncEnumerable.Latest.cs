namespace Linx.AsyncEnumerable
{
    using Observable;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source) => source.ToLinxObservable().Latest();

        /// <summary>
        /// Ignores all but the latest <paramref name="max"/> elements.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source, int max) => source.ToLinxObservable().Latest(max);
    }
}