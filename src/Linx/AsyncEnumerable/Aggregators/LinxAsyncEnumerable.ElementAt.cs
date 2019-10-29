namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than or equal to the number of elements in <paramref name="source"/>.</exception>
        public static async Task<T> ElementAt<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token)
        {
            var (b, v) = await source.ElementAtMaybe(index, token).ConfigureAwait(false);
            return b ? v : throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Returns the element at a specified index in a sequence, or a default value.
        /// </summary>
        public static async Task<T> ElementAtOrDefault<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token)
        {
            return (await source.ElementAtMaybe(index, token).ConfigureAwait(false)).Value;
        }

        /// <summary>
        /// Returns the element at a specified index in a sequence, or a default value.
        /// </summary>
        public static async Task<T?> ElementAtOrNull<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token) where T : struct
        {
            var (b, v) = await source.ElementAtMaybe(index, token).ConfigureAwait(false);
            return b ? v : default(T?);
        }

        private static async Task<(bool HasValue, T Value)> ElementAtMaybe<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();
            if (index < 0) return default;

            var i = 0;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                if (i == index) return (true, item);
                i++;
            }
            return default;
        }
    }
}
