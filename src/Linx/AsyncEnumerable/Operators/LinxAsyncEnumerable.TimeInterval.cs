namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using Observable;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Records the time interval between consecutive values.
        /// </summary>
        public static ILinxObservable<TimeInterval<T>> TimeInterval<T>(this IAsyncEnumerable<T> source) => source.ToLinxObservable().TimeInterval();
    }
}
