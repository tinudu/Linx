namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the first element of a sequence.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static async Task<T> First<T>(this IAsyncEnumerableObs<T> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try { return await ae.MoveNextAsync() ? ae.Current : throw new InvalidOperationException(Strings.SequenceContainsNoElement); }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
        public static async Task<T> First<T>(this IAsyncEnumerableObs<T> source, Func<T, bool> predicate, CancellationToken token)
            => await source.Where(predicate).First(token).ConfigureAwait(false);
    }
}
