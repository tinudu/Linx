namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
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
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                yield return element;
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return item;
            }
        }
    }
}
