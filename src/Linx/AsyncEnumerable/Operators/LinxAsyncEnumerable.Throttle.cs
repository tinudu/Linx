namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores values which are followed by another value before the specified interval in milliseconds.
        /// </summary>
        public static IAsyncEnumerable<T> Throttle<T>(this IAsyncEnumerable<T> source, int intervalMilliseconds)
            => source.Throttle(TimeSpan.FromMilliseconds(intervalMilliseconds));

        /// <summary>
        /// Ignores values which are followed by another value within the specified interval.
        /// </summary>
        public static IAsyncEnumerable<T> Throttle<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
            => throw new NotImplementedException();
    }
}
