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
    /// Concats the elements of the specified sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources)
    {
        if (sources == null) throw new ArgumentNullException(nameof(sources));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var outer in sources.WithCancellation(token).ConfigureAwait(false))
                await foreach (var inner in outer.WithCancellation(token).ConfigureAwait(false))
                    yield return inner;
        }
    }

    /// <summary>
    /// Concats the elements of the specified sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Concat<T>(this IEnumerable<IAsyncEnumerable<T>> sources)
    {
        if (sources == null) throw new ArgumentNullException(nameof(sources));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var outer in sources)
                await foreach (var inner in outer.WithCancellation(token).ConfigureAwait(false))
                    yield return inner;
        }
    }

    /// <summary>
    /// Concats the elements of the specified sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));
        return new[] { first, second }.Concat();
    }

    /// <summary>
    /// Concats the elements of the specified sequences.
    /// </summary>
    public static IAsyncEnumerable<T> Concat<T>(this IAsyncEnumerable<T> source, params IAsyncEnumerable<T>[] sources)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (sources == null) throw new ArgumentNullException(nameof(sources));
        return sources.Prepend(source).Concat();
    }
}
