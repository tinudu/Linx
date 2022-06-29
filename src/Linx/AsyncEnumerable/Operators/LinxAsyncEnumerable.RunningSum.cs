using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets the running sum. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<int> RunningSum32(this IAsyncEnumerable<int> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<int> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            var sum = 0;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                checked { sum += item; }
                yield return sum;
            }
        }
    }

    /// <summary>
    /// Gets the running sum. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<long> RunningSum64(this IAsyncEnumerable<int> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<long> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            var sum = 0L;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                checked { sum += item; }
                yield return sum;
            }
        }
    }

    /// <summary>
    /// Gets the running sum. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<long> RunningSum(this IAsyncEnumerable<long> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<long> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            var sum = 0L;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                checked { sum += item; }
                yield return sum;
            }
        }
    }

    /// <summary>
    /// Gets the running sum. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<float> RunningSum(this IAsyncEnumerable<float> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<float> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            var sum = 0F;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                checked { sum += item; }
                yield return sum;
            }
        }
    }

    /// <summary>
    /// Gets the running sum. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<double> RunningSum(this IAsyncEnumerable<double> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<double> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            var sum = 0D;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                checked { sum += item; }
                yield return sum;
            }
        }
    }

    /// <summary>
    /// Gets the running sum. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<decimal> RunningSum(this IAsyncEnumerable<decimal> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<decimal> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            var sum = 0M;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
            {
                checked { sum += item; }
                yield return sum;
            }
        }
    }
}
