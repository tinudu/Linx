using System;

namespace Linx.AsyncEnumerable;

/// <summary>
/// Exception thrown when <see cref="System.Collections.Generic.IAsyncEnumerator{T}.MoveNextAsync"/> is called after the enumerator was disposed.
/// </summary>
public sealed class AsyncEnumeratorDisposedException : ObjectDisposedException
{
    /// <summary>
    /// Singleton.
    /// </summary>
    public static AsyncEnumeratorDisposedException Instance { get; } = new AsyncEnumeratorDisposedException();

    private AsyncEnumeratorDisposedException() : base("IAsyncEnumerator<>") { }
}

/// <summary>
/// Exception thrown when a <see cref="ISubject{T}"/> was already connected.
/// </summary>
public sealed class SubjectAlreadyConnectedException : InvalidOperationException
{
    /// <summary>
    /// Singleton.
    /// </summary>
    public static SubjectAlreadyConnectedException Instance { get; } = new SubjectAlreadyConnectedException();

    private SubjectAlreadyConnectedException() : base("ISubject<> is already connected.") { }
}

/// <summary>
/// Exception thrown when a <see cref="ISubject{T}"/> was disposed.
/// </summary>
public sealed class SubjectDisposedException : ObjectDisposedException
{
    /// <summary>
    /// Singleton.
    /// </summary>
    public static SubjectDisposedException Instance { get; } = new();

    private SubjectDisposedException() : base("ISubject<>") { }
}
