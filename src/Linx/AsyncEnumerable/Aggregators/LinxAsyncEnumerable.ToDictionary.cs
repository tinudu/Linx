namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Aggregate to a dictionary.
        /// </summary>
        public static async Task<IDictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            CancellationToken token,
            IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            token.ThrowIfCancellationRequested();

            var result = new Dictionary<TKey, TSource>(comparer);
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                result.Add(keySelector(item), item);
            return result;
        }

        /// <summary>
        /// Aggregate to a dictionary.
        /// </summary>
        public static async Task<IDictionary<TKey, TValue>> ToDictionary<TSource, TKey, TValue>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            CancellationToken token,
            IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
            token.ThrowIfCancellationRequested();

            var result = new Dictionary<TKey, TValue>(comparer);
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                result.Add(keySelector(item), valueSelector(item));
            return result;
        }

        /// <summary>
        /// Aggregate to a dictionary.
        /// </summary>
        public static async Task<IDictionary<TKey, TValue>> ToDictionary<TKey, TValue>(
            this IAsyncEnumerable<KeyValuePair<TKey, TValue>> source,
            CancellationToken token,
            IEqualityComparer<TKey>? comparer = null) where TKey : notnull
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var result = new Dictionary<TKey, TValue>(comparer);
            await foreach (var kv in source.WithCancellation(token).ConfigureAwait(false))
                result.Add(kv.Key, kv.Value);
            return result;
        }
    }
}
