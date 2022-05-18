using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Returns the elements of the specified sequence or the type parameter's default value in a singleton sequence if the sequence is empty.
    /// </summary>
    public static IAsyncEnumerable<T?> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source) => source.DefaultIfEmpty(default);

    /// <summary>
    /// Returns the elements of the specified sequence or the specified <paramref name="default"/>, if the sequence is empty.
    /// </summary>
    public static IAsyncEnumerable<T?> DefaultIfEmpty<T>(this IAsyncEnumerable<T> source, T? @default)
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
                yield return @default;
        }
    }
}
