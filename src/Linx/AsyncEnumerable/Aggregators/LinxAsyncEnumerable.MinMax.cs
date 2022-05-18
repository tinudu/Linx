using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns the minimum non-null element of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
    public static async Task<T> Min<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null)
    {
        var (b, v) = await source.MinMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the minimum non-null element of a projection of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
    public static async Task<TResult> Min<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult>? comparer = null)
    {
        var (b, v) = await source.Select(selector).MinMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the minimum non-null element of a sequence, or a default value.
    /// </summary>
    public static async Task<T> MinOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null)
    {
        return (await source.MinMaybe(token, comparer).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the minimum non-null element of a projection of a sequence, or a default value.
    /// </summary>
    public static async Task<TResult> MinOrDefault<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult>? comparer = null)
    {
        return (await source.Select(selector).MinMaybe(token, comparer).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the minimum element of a sequence, or a default value.
    /// </summary>
    public static async Task<T?> MinOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null) where T : struct
    {
        var (b, v) = await source.MinMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the minimum element of a projection of a sequence, or a default value.
    /// </summary>
    public static async Task<TResult?> MinOrNull<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult>? comparer = null) where TResult : struct
    {
        var (b, v) = await source.Select(selector).MinMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : default(TResult?);
    }

    /// <summary>
    /// Returns the elements of a sequence with the minimum non-null key.
    /// </summary>
    public static async Task<IList<TSource>> MinBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken token, IComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (comparer == null) comparer = Comparer<TKey>.Default;
        token.ThrowIfCancellationRequested();

        await using var ae = source.Select(x => (key: keySelector(x), value: x)).Where(x => x.key is not null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        if (!await ae.MoveNextAsync())
            return Array.Empty<TSource>();
        var item = ae.Current;
        var min = item.key;
        var result = new List<TSource> { item.value };
        while (await ae.MoveNextAsync())
        {
            item = ae.Current;
            var cmp = comparer.Compare(item.key, min);
            if (cmp > 0)
                continue;
            if (cmp < 0)
            {
                min = item.key;
                result.Clear();
            }
            result.Add(item.value);
        }
        return result;
    }

    private static async Task<(bool HasValue, T Value)> MinMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null)
    {
        if (comparer == null) comparer = Comparer<T>.Default;
        token.ThrowIfCancellationRequested();

        await using var ae = source.Where(x => x is not null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        if (!await ae.MoveNextAsync()) return default;
        var min = ae.Current;
        while (await ae.MoveNextAsync())
        {
            var current = ae.Current;
            if (comparer.Compare(current, min) < 0) min = current;
        }
        return (true, min);
    }

    /// <summary>
    /// Returns the maximum non-null element of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
    public static async Task<T> Max<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null)
    {
        var (b, v) = await source.MaxMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the maximum non-null element of a projection of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no non-null elements.</exception>
    public static async Task<TResult> Max<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult>? comparer = null)
    {
        var (b, v) = await source.Select(selector).MaxMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the maximum non-null element of a sequence, or a default value.
    /// </summary>
    public static async Task<T> MaxOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null)
    {
        return (await source.MaxMaybe(token, comparer).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the maximum non-null element of a projection of a sequence, or a default value.
    /// </summary>
    public static async Task<TResult> MaxOrDefault<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult>? comparer = null)
    {
        return (await source.Select(selector).MaxMaybe(token, comparer).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the maximum element of a sequence, or a default value.
    /// </summary>
    public static async Task<T?> MaxOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null) where T : struct
    {
        var (b, v) = await source.MaxMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the maximum element of a projection of a sequence, or a default value.
    /// </summary>
    public static async Task<TResult?> MaxOrNull<TSource, TResult>(this IAsyncEnumerable<TSource> source, Func<TSource, TResult> selector, CancellationToken token, IComparer<TResult>? comparer = null) where TResult : struct
    {
        var (b, v) = await source.Select(selector).MaxMaybe(token, comparer).ConfigureAwait(false);
        return b ? v : default(TResult?);
    }

    /// <summary>
    /// Returns the elements of a sequence with the maximum non-null key.
    /// </summary>
    public static async Task<IList<TSource>> MaxBy<TSource, TKey>(this IAsyncEnumerable<TSource> source, Func<TSource, TKey> keySelector, CancellationToken token, IComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        if (comparer == null) comparer = Comparer<TKey>.Default;
        token.ThrowIfCancellationRequested();

        await using var ae = source.Select(x => (key: keySelector(x), value: x)).Where(x => x.key is not null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        if (!await ae.MoveNextAsync())
            return Array.Empty<TSource>();
        var item = ae.Current;
        var max = item.key;
        var result = new List<TSource> { item.value };
        while (await ae.MoveNextAsync())
        {
            item = ae.Current;
            var cmp = comparer.Compare(item.key, max);
            if (cmp < 0)
                continue;
            if (cmp > 0)
            {
                max = item.key;
                result.Clear();
            }
            result.Add(item.value);
        }
        return result;
    }

    private static async Task<(bool HasValue, T Value)> MaxMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token, IComparer<T>? comparer = null)
    {
        if (comparer == null) comparer = Comparer<T>.Default;
        token.ThrowIfCancellationRequested();

        await using var ae = source.Where(x => x is not null).WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        if (!await ae.MoveNextAsync()) return default;
        var max = ae.Current;
        while (await ae.MoveNextAsync())
        {
            var current = ae.Current;
            if (comparer.Compare(current, max) > 0) max = current;
        }
        return (true, max);
    }

}