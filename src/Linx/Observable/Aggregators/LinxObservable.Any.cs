namespace Linx.Observable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Determines whether the sequence contains any element.
        /// </summary>
        public static async Task<bool> Any<T>(
            this ILinxObservable<T> source, 
            CancellationToken token) =>
            await source.Aggregate(false, (a, c) => (true, false), token).ConfigureAwait(false);

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        public static async Task<bool> Any<T>(
            this ILinxObservable<T> source,
            Func<T, bool> predicate,
            CancellationToken token) =>
            await source.Where(predicate).Any(token).ConfigureAwait(false);

    }
}