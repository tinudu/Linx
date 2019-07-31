namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Observable;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores values which are followed by another value before the specified interval in milliseconds.
        /// </summary>
        public static ILinxObservable<T> Throttle<T>(this IAsyncEnumerable<T> source, int intervalMilliseconds)
            => source.ToLinxObservable().Throttle(intervalMilliseconds);

        /// <summary>
        /// Ignores values which are followed by another value within the specified interval.
        /// </summary>
        public static ILinxObservable<T> Throttle<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
            => source.ToLinxObservable().Throttle(interval);
    }
}
