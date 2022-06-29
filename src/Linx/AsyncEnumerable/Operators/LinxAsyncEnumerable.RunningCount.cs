using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets the running count. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<int> RunningCount32<T>(this IAsyncEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<int> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            await using var e = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            var count = 0;
            while (await e.MoveNextAsync())
                yield return checked(++count);
        }
    }

    /// <summary>
    /// Gets the running count. Starts with 0.
    /// </summary>
    public static IAsyncEnumerable<long> RunningCount64<T>(this IAsyncEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<long> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            yield return 0;
            await using var e = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            var count = 0L;
            while (await e.MoveNextAsync())
                yield return checked(++count);
        }
    }
}
