﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Correlates the elements of two sequences based on key equality, and groups the results.
    /// </summary>
    public static IAsyncEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
        this IAsyncEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (outer == null) throw new ArgumentNullException(nameof(outer));
        if (inner == null) throw new ArgumentNullException(nameof(inner));
        if (outerKeySelector == null) throw new ArgumentNullException(nameof(outerKeySelector));
        if (innerKeySelector == null) throw new ArgumentNullException(nameof(innerKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
        return Iterator();

        async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var innerItems = inner
                .Select(i => (key: innerKeySelector(i), value: i))
                .Where(kv => kv.key != null)
                .ToLookup(kv => kv.key, kv => kv.value, comparer);

            await foreach (var item in outer.WithCancellation(token).ConfigureAwait(false))
                yield return resultSelector(item, innerItems[outerKeySelector(item)]);
        }
    }
}
