﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Produces the set difference of two sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Except<T>(this IAsyncEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T>? comparer = null)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));
        if (comparer == null) comparer = EqualityComparer<T>.Default;

        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var excluded = second is HashSet<T> h && h.Comparer == comparer ? h : new HashSet<T>(second, comparer);
            var included = new HashSet<T>(comparer);
            await foreach (var item in first.WithCancellation(token).ConfigureAwait(false))
                if (!excluded.Contains(item) && included.Add(item))
                    yield return item;
        }
    }
}
