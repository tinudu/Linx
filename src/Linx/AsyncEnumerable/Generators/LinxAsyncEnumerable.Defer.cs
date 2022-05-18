using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns a sequence that invokes the factory whenever it is enumerated.
    /// </summary>
    public static IAsyncEnumerable<T> Defer<T>(Func<IAsyncEnumerable<T>> getSource)
    {
        if (getSource == null) throw new ArgumentNullException(nameof(getSource));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var source = getSource();
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return item;
        }
    }

    /// <summary>
    /// Returns a sequence that invokes the factory whenever it is enumerated.
    /// </summary>
    public static IAsyncEnumerable<T> DeferAwait<T>(Func<CancellationToken, Task<IAsyncEnumerable<T>>> getSourceAsync)
    {
        if (getSourceAsync == null) throw new ArgumentNullException(nameof(getSourceAsync));
        return Iterator();

        async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            var source = await getSourceAsync(token).ConfigureAwait(false);
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                yield return item;
        }
    }
}
