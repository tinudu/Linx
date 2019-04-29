namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the only element of a sequence, and throws an exception if there is not exactly one element in the sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no or multiple elements.</exception>
        public static async Task<T> Single<T>(this IAsyncEnumerableObs<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                if (!await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsNoElement);
                var single = ae.Current;
                if (await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
                return single;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Returns the only element of a sequence that satisfies a specified condition, and throws an exception if not exactly one such element exists.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no or multiple elements.</exception>
        public static async Task<T> Single<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).Single(token).ConfigureAwait(false);
    }
}
