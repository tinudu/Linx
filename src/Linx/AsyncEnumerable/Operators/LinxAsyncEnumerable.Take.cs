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
        /// Returns a specified number of contiguous elements from the start of a sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Take<T>(this IAsyncEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return Empty<T>();
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var remaining = count;
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                {
                    yield return item;
                    if (--remaining == 0)
                        break;
                }
            }
        }
    }
}
