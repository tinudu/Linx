﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Aggregate elements into an array.
    /// </summary>
    public static async ValueTask<T[]> ToArray<T>(this IAsyncEnumerable<T> source, CancellationToken token) => (await source.ToList(token).ConfigureAwait(false)).ToArray();
}
