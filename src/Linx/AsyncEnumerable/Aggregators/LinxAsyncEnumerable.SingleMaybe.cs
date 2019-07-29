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
        /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
        public static async Task<Maybe<T>> SingleMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                if (!await ae.MoveNextAsync()) return default;
                var single = ae.Current;
                if (!await ae.MoveNextAsync()) return single;
                throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Returns the single element of a sequence that satisfies a condition, if any.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
        public static async Task<Maybe<T>> SingleOrDefault<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).SingleMaybe(token).ConfigureAwait(false);
    }
}
