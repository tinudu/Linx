using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Async;

/// <summary>
/// Represents the producer side of a <see cref="ValueTask{T}"/>.
/// </summary>
public interface IValueTaskCompleter<T>
{
    /// <summary>
    /// Gets the <see cref="ValueTask{T}"/> controlled by this instance.
    /// </summary>
    ValueTask<T> ValueTask { get; }

    /// <summary>
    /// Gets a <see cref="System.Threading.Tasks.ValueTask"/> controlled by this instance.
    /// </summary>
    ValueTask NonGenericValueTask { get; }

    /// <summary>
    /// Set <see cref="ValueTaskSourceStatus.Succeeded"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Already completed.</exception>
    void SetResult(T value);

    /// <summary>
    /// Set <see cref="ValueTaskSourceStatus.Faulted"/> or <see cref="ValueTaskSourceStatus.Canceled"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Already completed.</exception>
    void SetException(Exception exception);
}
