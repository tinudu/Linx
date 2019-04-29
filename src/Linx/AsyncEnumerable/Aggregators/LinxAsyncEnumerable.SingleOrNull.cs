namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the only element of a sequence, or null if the sequence is empty; this method throws an exception if there is more than one element in the sequence.
        /// </summary>
        public static async Task<T?> SingleOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var single = ae.Current;
                if (await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
                return single;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition or null if no such element exists; this method throws an exception if more than one element satisfies the condition.
        /// </summary>
        public static async Task<T?> SingleOrNull<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
            => await source.Where(predicate).SingleOrNull(token).ConfigureAwait(false);
    }
}
