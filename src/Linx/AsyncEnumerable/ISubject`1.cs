using System.Collections.Generic;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

/// <summary>
/// Object exposing a <see cref="IAsyncEnumerable{T}"/> that does not produce items until it is connected to a source.
/// </summary>
/// <remarks>After it's connected, it will automatically enumerate the source as long as there are subscribers.</remarks>
public interface ISubject<T>
{
    /// <summary>
    /// Gets a <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <remarks>
    /// Disposes itself when no more enumerators are present.
    /// Thereafter, <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(System.Threading.CancellationToken)"/> throws a <see cref="SubjectDisposedException"/>
    /// </remarks>
    public IAsyncEnumerable<T> AsyncEnumerable { get; }

    /// <summary>
    /// Connect to a source.
    /// </summary>
    /// <exception cref="SubjectAlreadyConnectedException"><see cref="Connect()"/> was called before.</exception>
    /// <returns>A <see cref="Task"/> that completes when disconnected.</returns>
    void Connect();
}
