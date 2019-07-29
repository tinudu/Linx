namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the single element of a sequence, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no or multiple elements.</exception>
        public static async Task<T> Single<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsNoElement);
                var single = ae.Current;
                if (await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
                return single;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the single element of a sequence that satisfies a condition, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no or multiple elements.</exception>
        public static Task<T> Single<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).Single(token); }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }
    }
}
