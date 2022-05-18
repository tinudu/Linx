using System;
using System.Collections.Generic;
using System.Threading;

namespace Linx.AsyncEnumerable;

internal sealed class AnonymousAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Func<CancellationToken, IAsyncEnumerator<T>> _getEnumerator;

    public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator)
    {
        if (getEnumerator is null) throw new ArgumentNullException(nameof(getEnumerator));
        _getEnumerator = getEnumerator;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => _getEnumerator(token);
}