namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Prepends a value to the end of the sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Prepend<T>(this IAsyncEnumerable<T> source, T element)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                yield return element;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return item;
            }
        }
    }
}
