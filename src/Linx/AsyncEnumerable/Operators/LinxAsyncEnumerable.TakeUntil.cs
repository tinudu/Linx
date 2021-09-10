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
        /// Returns values until the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> TakeUntil<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                {
                    yield return item;
                    if (predicate(item))
                        break;
                }
            }
        }

        /// <summary>
        /// Returns values until the specified condition is true.
        /// </summary>
        public static IAsyncEnumerable<T> TakeUntil<T>(this IAsyncEnumerable<T> source, Func<T, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var i = 0;
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                {
                    if (predicate(item, unchecked(i++)))
                        break;
                    yield return item;
                }
            }
        }
    }
}
