namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        public static async Task<bool> Any<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try { return await ae.MoveNextAsync(); }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Determines whether any element of a sequence satisfies a condition.
        /// </summary>
        public static Task<bool> Any<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).Any(token); }
            catch (Exception ex) { return Task.FromException<bool>(ex); }
        }
    }
}
