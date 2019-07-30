namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the last element of a sequence, if any.
        /// </summary>
        public static async Task<Maybe<T>> LastMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var last = ae.Current;
                while (await ae.MoveNextAsync()) last = ae.Current;
                return last;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the last element of a sequence that satisfies a condition, if any.
        /// </summary>
        public static Task<Maybe<T>> LastMaybe<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
        {
            try { return source.Where(predicate).LastMaybe(token); }
            catch (Exception ex) { return Task.FromException<Maybe<T>>(ex); }
        }
    }
}
