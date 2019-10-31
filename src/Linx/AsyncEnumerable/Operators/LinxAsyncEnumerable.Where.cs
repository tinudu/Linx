namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    if (predicate(item))
                        yield return item;
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    if (predicate(item, i++))
                        yield return item;
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, CancellationToken, Task<bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    if (await predicate(item, token).ConfigureAwait(false))
                        yield return item;
            }
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, int, CancellationToken, Task<bool>> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    if (await predicate(item, i++, token).ConfigureAwait(false))
                        yield return item;
            }
        }
    }
}
