namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using Observable;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Records the timestamp for each value.
        /// </summary>
        public static ILinxObservable<Timestamped<T>> Timestamp<T>(this IAsyncEnumerable<T> source) => source.ToLinxObservable().Timestamp();
    }
}
