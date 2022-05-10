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
        /// Returns distinct elements from a sequence
        /// </summary>
        public static IAsyncEnumerable<T> Distinct<T>(this IAsyncEnumerable<T> source, IEqualityComparer<T>? comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();

                var distinct = new HashSet<T>(comparer);
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    if (distinct.Add(item))
                        yield return item;
            }
        }
    }
}
