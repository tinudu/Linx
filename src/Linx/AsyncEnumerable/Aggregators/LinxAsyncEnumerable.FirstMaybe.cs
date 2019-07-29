namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the first element of a sequence, if any.
        /// </summary>
        public static async Task<Maybe<T>> FirstMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try { return await ae.MoveNextAsync() ? ae.Current : default; }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the first element of the sequence that satisfies a condition, if any.
        /// </summary>
        public static Task<Maybe<T>> FirstMaybe<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).FirstMaybe(token); }
            catch (Exception ex) { return Task.FromException<Maybe<T>>(ex); }
        }
    }
}
