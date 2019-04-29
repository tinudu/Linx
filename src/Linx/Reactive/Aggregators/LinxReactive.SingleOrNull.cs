namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the only element of a sequence, or null if the sequence is empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        public static async Task<T?> SingleOrNull<T>(this IAsyncEnumerableObs<T> source, CancellationToken token) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var single = ae.Current;
                if (await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
                return single;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or null if no such element exists; this method throws an exception if more than one element satisfies the condition.
        /// </summary>
        public static async Task<T?> SingleOrNull<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
            => await source.Where(predicate).SingleOrNull(token).ConfigureAwait(false);
    }
}
