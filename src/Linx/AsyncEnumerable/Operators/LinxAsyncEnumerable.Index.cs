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
    public static IAsyncEnumerable<KeyValuePair<int, T>> Index32<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<KeyValuePair<int, T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var index = -1;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return new KeyValuePair<int, T>(checked(++index), item);
        }
    }

    /// <summary>
    /// Decorate each element in <paramref name="source"/> with its zero based index.
    /// </summary>
    public static IAsyncEnumerable<KeyValuePair<long, T>> Index64<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<KeyValuePair<long, T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var index = -1L;
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return new KeyValuePair<long, T>(checked(++index), item);
        }
    }
}
