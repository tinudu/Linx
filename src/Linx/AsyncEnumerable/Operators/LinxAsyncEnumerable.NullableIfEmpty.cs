using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns the elements of the specified sequence or the default <see cref="Nullable{T}"/>.
    /// </summary>
    public static IAsyncEnumerable<T?> NullIfEmpty<T>(this IAsyncEnumerable<T> source) where T : struct
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return Iterator();

        async IAsyncEnumerable<T?> Iterator([EnumeratorCancellation] CancellationToken token = default)
        {
            await using var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            if (await ae.MoveNextAsync())
                do yield return ae.Current;
                while (await ae.MoveNextAsync());
            else
                yield return null;
        }
    }
}
