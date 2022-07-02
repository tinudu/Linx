using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Linx.Observable;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Convert to a <see cref="ILinxObservable{T}"/>.
    /// </summary>
    public static ILinxObservable<T> ToObservable<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        return LinxObservable.Create<T>(async (yield, token) =>
        {
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                if (!yield(item))
                    return;
        });
    }
}
