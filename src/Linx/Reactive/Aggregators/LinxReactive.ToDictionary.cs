﻿namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Aggregate to a dictionary.
        /// </summary>
        public static async Task<IDictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
            this IAsyncEnumerableObs<TSource> source,
            Func<TSource, TKey> keySelector,
            CancellationToken token,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var dictionary = new Dictionary<TKey, TSource>(comparer ?? EqualityComparer<TKey>.Default);
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    dictionary.Add(keySelector(current), current);
                }

                return dictionary;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }

        /// <summary>
        /// Aggregate to a dictionary.
        /// </summary>
        public static async Task<IDictionary<TKey, TValue>> ToDictionary<TSource, TKey, TValue>(
            this IAsyncEnumerableObs<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            CancellationToken token,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var dictionary = new Dictionary<TKey, TValue>(comparer ?? EqualityComparer<TKey>.Default);
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    dictionary.Add(keySelector(current), valueSelector(current));
                }

                return dictionary;
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
