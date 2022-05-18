using System;

namespace Linx;

/// <summary>
/// Exception thrown when <see cref="System.Collections.Generic.IAsyncEnumerator{T}.MoveNextAsync"/> is called after the enumerator was disposed.
/// </summary>
public sealed class AsyncEnumeratorDisposedException : ObjectDisposedException
{
    /// <summary>
    /// Singleton.
    /// </summary>
    public static AsyncEnumeratorDisposedException Instance { get; } = new AsyncEnumeratorDisposedException();

    private AsyncEnumeratorDisposedException() : base("IAsyncEnumerator") { }
}

/// <summary>
/// Exception thrown when <see cref="System.Collections.Generic.IAsyncEnumerator{T}.MoveNextAsync"/> is called after the enumerator was disposed.
/// </summary>
public sealed class AlreadyConnectedException : InvalidOperationException
{
    /// <summary>
    /// Singleton.
    /// </summary>
    public static AlreadyConnectedException Instance { get; } = new AlreadyConnectedException();

    private AlreadyConnectedException() : base("Already connected.") { }
}
