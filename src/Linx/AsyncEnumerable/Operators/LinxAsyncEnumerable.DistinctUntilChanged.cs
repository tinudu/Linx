namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns distinct elements from a sequence
        /// </summary>
        public static IAsyncEnumerable<T> DistinctUntilChanged<T>(this IAsyncEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                if (!await ae.MoveNextAsync())
                    yield break;
                var prev = ae.Current;
                yield return prev;
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    if(comparer.Equals(current,prev))
                        continue;
                    prev = current;
                }
            }
        }
    }
}
