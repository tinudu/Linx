using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Create a <see cref="IAsyncEnumerable{T}"/> defined by it's <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
    /// </summary>
    public static IAsyncEnumerable<T> Create<T>(Func<CancellationToken, IAsyncEnumerator<T>> getAsyncEnumerator)
    {
        if (getAsyncEnumerator is null) throw new ArgumentNullException(nameof(getAsyncEnumerator));
        return new AnonymousAsyncEnumerable<T>(getAsyncEnumerator);
    }

    /// <summary>
    /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProduceAsyncDelegate{T}"/> coroutine.
    /// </summary>
    public static IAsyncEnumerable<T> Create<T>(ProduceAsyncDelegate<T> produceAsync, [CallerMemberName] string? displayName = default)
    {
        if (produceAsync == null) throw new ArgumentNullException(nameof(produceAsync));
        return new CoroutineIterator<T>(produceAsync, displayName);
    }

    /// <summary>
    /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProduceAsyncDelegate{T}"/> coroutine.
    /// </summary>
    public static IAsyncEnumerable<T> Create<T>(T _, ProduceAsyncDelegate<T> produceAsync, [CallerMemberName] string? displayName = default)
    {
        if (produceAsync == null) throw new ArgumentNullException(nameof(produceAsync));
        return new CoroutineIterator<T>(produceAsync, displayName);
    }
}
