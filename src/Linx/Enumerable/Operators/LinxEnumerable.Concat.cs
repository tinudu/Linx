namespace Linx.Enumerable
{
    using AsyncEnumerable;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Concats the elements of the specified sequences.
        /// </summary>
        public static IAsyncEnumerable<T> Concat<T>(this IEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var outer in sources)
                    await foreach (var inner in outer.WithCancellation(token).ConfigureAwait(false))
                        yield return inner;
            }
        }
    }
}
