using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<T> Iterator()
        {
            foreach (var item in source)
                yield return item;

            await Task.CompletedTask.ConfigureAwait(false); // prevent CS1998
        }
    }
}
