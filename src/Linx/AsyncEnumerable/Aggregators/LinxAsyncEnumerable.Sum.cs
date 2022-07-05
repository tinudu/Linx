using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Computes the sum of a sequence values.
    /// </summary>
    public static async ValueTask<int> Sum(this IAsyncEnumerable<int> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        var sum = 0;
        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            checked { sum += item; }
        return sum;
    }

    /// <summary>
    /// Computes the sum of the non-null values of a sequence.
    /// </summary>
    public static async ValueTask<int> Sum(this IAsyncEnumerable<int?> source, CancellationToken token)
        => await source.Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<int> Sum<T>(this IAsyncEnumerable<T> source, Func<T, int> selector, CancellationToken token)
        => await source.Select(selector).Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of the non-null values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<int> Sum<T>(this IAsyncEnumerable<T> source, Func<T, int?> selector, CancellationToken token)
        => await source.Select(selector).Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of a sequence values.
    /// </summary>
    public static async ValueTask<long> Sum(this IAsyncEnumerable<long> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        var sum = 0L;
        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            checked { sum += item; }
        return sum;
    }

    /// <summary>
    /// Computes the sum of the non-null values of a sequence.
    /// </summary>
    public static async ValueTask<long> Sum(this IAsyncEnumerable<long?> source, CancellationToken token)
        => await source.Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<long> Sum<T>(this IAsyncEnumerable<T> source, Func<T, long> selector, CancellationToken token)
        => await source.Select(selector).Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of the non-null values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<long> Sum<T>(this IAsyncEnumerable<T> source, Func<T, long?> selector, CancellationToken token)
        => await source.Select(selector).Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of a sequence values.
    /// </summary>
    public static async ValueTask<double> Sum(this IAsyncEnumerable<double> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        var sum = 0D;
        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            sum += item;
        return sum;
    }

    /// <summary>
    /// Computes the sum of the non-null values of a sequence.
    /// </summary>
    public static async ValueTask<double> Sum(this IAsyncEnumerable<double?> source, CancellationToken token)
        => await source.Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<double> Sum<T>(this IAsyncEnumerable<T> source, Func<T, double> selector, CancellationToken token)
        => await source.Select(selector).Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of the non-null values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<double> Sum<T>(this IAsyncEnumerable<T> source, Func<T, double?> selector, CancellationToken token)
        => await source.Select(selector).Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of a sequence values.
    /// </summary>
    public static async ValueTask<float> Sum(this IAsyncEnumerable<float> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        var sum = 0F;
        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            sum += item;
        return sum;
    }

    /// <summary>
    /// Computes the sum of the non-null values of a sequence.
    /// </summary>
    public static async ValueTask<float> Sum(this IAsyncEnumerable<float?> source, CancellationToken token)
        => await source.Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<float> Sum<T>(this IAsyncEnumerable<T> source, Func<T, float> selector, CancellationToken token)
        => await source.Select(selector).Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of the non-null values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<float> Sum<T>(this IAsyncEnumerable<T> source, Func<T, float?> selector, CancellationToken token)
        => await source.Select(selector).Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of a sequence values.
    /// </summary>
    public static async ValueTask<decimal> Sum(this IAsyncEnumerable<decimal> source, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        token.ThrowIfCancellationRequested();

        var sum = 0M;
        await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            sum += item;
        return sum;
    }

    /// <summary>
    /// Computes the sum of the non-null values of a sequence.
    /// </summary>
    public static async ValueTask<decimal> Sum(this IAsyncEnumerable<decimal?> source, CancellationToken token)
        => await source.Values().Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<decimal> Sum<T>(this IAsyncEnumerable<T> source, Func<T, decimal> selector, CancellationToken token)
        => await source.Select(selector).Sum(token).ConfigureAwait(false);

    /// <summary>
    /// Computes the sum of the sequence of the non-null values that are obtained by invoking a transform function on each element of the input sequence.
    /// </summary>
    public static async ValueTask<decimal> Sum<T>(this IAsyncEnumerable<T> source, Func<T, decimal?> selector, CancellationToken token)
        => await source.Select(selector).Values().Sum(token).ConfigureAwait(false);

}

