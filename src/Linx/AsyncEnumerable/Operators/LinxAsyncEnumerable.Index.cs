using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Decorate each element in <paramref name="source"/> with its zero based index.
    /// </summary>
    public static IAsyncEnumerable<KeyValuePair<int, T>> Index<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<KeyValuePair<int, T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await using var e = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();

            if (await e.MoveNextAsync())
                yield return new(0, e.Current);

            var index = 0;
            while (await e.MoveNextAsync())
            {
                checked { index++; }
                yield return new(index, e.Current);
            }
        }
    }

    /// <summary>
    /// Decorate each element in <paramref name="source"/> with its zero based index.
    /// </summary>
    public static IAsyncEnumerable<KeyValuePair<long, T>> IndexLong<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<KeyValuePair<long, T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await using var e = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();

            if (await e.MoveNextAsync())
                yield return new(0, e.Current);

            var index = 0L;
            while (await e.MoveNextAsync())
            {
                checked { index++; }
                yield return new(index, e.Current);
            }
        }
    }
}
