namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncEnumerable;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, CancellationToken, Task<bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                    if (await predicate(item, token).ConfigureAwait(false))
                        yield return item;
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, int, CancellationToken, Task<bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                    if (await predicate(item, i++, token).ConfigureAwait(false))
                        yield return item;
            }
        }
    }
}
