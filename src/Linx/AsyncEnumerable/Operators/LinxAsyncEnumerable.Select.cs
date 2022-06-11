using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Projects each element of a sequence into a new form.
    /// </summary>
    public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return Iterator();

        async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return selector(item);
        }
    }

    /// <summary>
    /// Projects each element of a sequence into a new form.
    /// </summary>
    public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, int, TResult> selector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return Iterator();

        async IAsyncEnumerable<TResult> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var i = 0;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return selector(item, unchecked(i++));
        }
    }
}
