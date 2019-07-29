namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        public static async Task<int> Count<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var count = 0;
                while (await ae.MoveNextAsync()) checked { count++; }
                return count;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        public static Task<int> Count<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).Count(token); }
            catch (Exception ex) { return Task.FromException<int>(ex); }
        }
    }
}
