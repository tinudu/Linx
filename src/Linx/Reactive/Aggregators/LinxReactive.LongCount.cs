namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        public static async Task<long> LongCount<T>(this IAsyncEnumerableObs<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var count = 0L;
                while (await ae.MoveNextAsync()) checked { count++; }
                return count;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Returns a number that represents how many elements in the specified sequence satisfy a condition.
        /// </summary>
        public static async Task<long> LongCount<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).LongCount(token);
    }
}
