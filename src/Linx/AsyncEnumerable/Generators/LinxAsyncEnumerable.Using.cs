using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Constructs a sequence that depends on a resource object.
    /// </summary>
    public static IAsyncEnumerable<T> Using<TResource, T>(
        Func<TResource> resourceFactory,
        Func<TResource, IAsyncEnumerable<T>> sequenceFactory)
        where TResource : IDisposable
    {
        if (resourceFactory == null) throw new ArgumentNullException(nameof(resourceFactory));
        if (sequenceFactory == null) throw new ArgumentNullException(nameof(sequenceFactory));

        return Create(GetEnumerator);

        async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
        {
            using var resource = resourceFactory();
            await foreach (var item in sequenceFactory(resource).WithCancellation(token).ConfigureAwait(false))
                yield return item;
        }
    }
}
