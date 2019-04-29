namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the first element of a sequence, or null if the sequence contains no elements.
        /// </summary>
        public static async Task<T?> FirstOrNull<T>(this IAsyncEnumerableObs<T> source, CancellationToken token) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try { return await ae.MoveNextAsync() ? ae.Current : default(T?); }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition or null if no such element is found.
        /// </summary>
        public static async Task<T?> FirstOrNull<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
            => await source.Where(predicate).FirstOrNull(token).ConfigureAwait(false);
    }
}
