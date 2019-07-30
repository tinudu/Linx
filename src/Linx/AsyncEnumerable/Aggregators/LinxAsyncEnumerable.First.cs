namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the first element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static async Task<T> First<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try { return await ae.MoveNextAsync() ? ae.Current : throw new InvalidOperationException(Strings.SequenceContainsNoElement); }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static Task<T> First<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).First(token); }
            catch (Exception ex) { return Task.FromException<T>(ex); }
        }
    }
}
