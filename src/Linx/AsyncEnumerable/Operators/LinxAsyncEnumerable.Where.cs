using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Filters a sequence of values based on a predicate.
    /// </summary>
    public static IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
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
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var i = 0;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                if (predicate(item, unchecked(i++)))
                    yield return item;
        }
    }

    /// <summary>
    /// Filters a sequence of values based on a predicate.
    /// </summary>
    public static IAsyncEnumerable<T> WhereAwait<T>(this IAsyncEnumerable<T> source, Func<T, int, CancellationToken, ValueTask<bool>> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var i = 0;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                if (await predicate(item, unchecked(i++), token).ConfigureAwait(false))
                    yield return item;
        }
    }

    /// <summary>
    /// Filters a sequence of values based on a predicate.
    /// </summary>
    public static IAsyncEnumerable<T> WhereAwait<T>(this IEnumerable<T> source, Func<T, CancellationToken, ValueTask<bool>> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var item in source)
                if (await predicate(item, token).ConfigureAwait(false))
                    yield return item;
        }
    }

    /// <summary>
    /// Filters a sequence of values based on a predicate.
    /// </summary>
    public static IAsyncEnumerable<T> WhereAwait<T>(this IEnumerable<T> source, Func<T, int, CancellationToken, ValueTask<bool>> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var i = 0;
            foreach (var item in source)
                if (await predicate(item, unchecked(i++), token).ConfigureAwait(false))
                    yield return item;
        }
    }
}
