using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns the first element of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
    public static async Task<T> First<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var (b, v) = await source.FirstMaybe(token).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies a condition.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
    public static async Task<T> First<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        var (b, v) = await source.Where(predicate).FirstMaybe(token).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value.
    /// </summary>
    public static async Task<T> FirstOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return (await source.FirstMaybe(token).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    public static async Task<T> FirstOrDefault<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        return (await source.Where(predicate).FirstMaybe(token).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the first element of a sequence, or a default value.
    /// </summary>
    public static async Task<T?> FirstOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var (b, v) = await source.FirstMaybe(token).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    public static async Task<T?> FirstOrNull<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
    {
        var (b, v) = await source.Where(predicate).FirstMaybe(token).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the last element of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
    public static async Task<T> Last<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var (b, v) = await source.LastMaybe(token).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the last element of a sequence that satisfies a condition.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no elements.</exception>
    public static async Task<T> Last<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        var (b, v) = await source.Where(predicate).LastMaybe(token).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the last element of a sequence, or a default value.
    /// </summary>
    public static async Task<T> LastOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return (await source.LastMaybe(token).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the last element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    public static async Task<T> LastOrDefault<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        return (await source.Where(predicate).LastMaybe(token).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the last element of a sequence, or a default value.
    /// </summary>
    public static async Task<T?> LastOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var (b, v) = await source.LastMaybe(token).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the last element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    public static async Task<T?> LastOrNull<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
    {
        var (b, v) = await source.Where(predicate).LastMaybe(token).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the single element of a sequence.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no or multiple elements.</exception>
    public static async Task<T> Single<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var (b, v) = await source.SingleMaybe(token).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the single element of a sequence that satisfies a condition.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains no or multiple elements.</exception>
    public static async Task<T> Single<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        var (b, v) = await source.Where(predicate).SingleMaybe(token).ConfigureAwait(false);
        return b ? v : throw new InvalidOperationException(Strings.SequenceContainsNoElement);
    }

    /// <summary>
    /// Returns the single element of a sequence, or a default value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
    public static async Task<T> SingleOrDefault<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return (await source.SingleMaybe(token).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the single element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
    public static async Task<T> SingleOrDefault<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token)
    {
        return (await source.Where(predicate).SingleMaybe(token).ConfigureAwait(false)).Value;
    }

    /// <summary>
    /// Returns the single element of a sequence, or a default value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
    public static async Task<T?> SingleOrNull<T>(this IAsyncEnumerable<T> source, CancellationToken token) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var (b, v) = await source.SingleMaybe(token).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    /// <summary>
    /// Returns the single element of a sequence that satisfies a condition, or a default value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Sequence contains multiple elements.</exception>
    public static async Task<T?> SingleOrNull<T>(this IAsyncEnumerable<T> source, Func<T, bool> predicate, CancellationToken token) where T : struct
    {
        var (b, v) = await source.Where(predicate).SingleMaybe(token).ConfigureAwait(false);
        return b ? v : default(T?);
    }

    private static async Task<(bool HasValue, T Value)> FirstMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        return await ae.MoveNextAsync() ? (true, ae.Current) : default;
    }

    private static async Task<(bool HasValue, T Value)> LastMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        if (!await ae.MoveNextAsync()) return default;
        var last = ae.Current;
        while (await ae.MoveNextAsync()) last = ae.Current;
        return (true, last);
    }

    private static async Task<(bool HasValue, T Value)> SingleMaybe<T>(this IAsyncEnumerable<T> source, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
        if (!await ae.MoveNextAsync()) return default;
        var single = ae.Current;
        if (await ae.MoveNextAsync()) throw new InvalidOperationException(Strings.SequenceContainsMultipleElements);
        return (true, single);
    }
}