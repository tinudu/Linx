using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Async;

/// <summary>
/// Represents the producer side of a <see cref="ValueTask"/>.
/// </summary>
public interface IValueTaskCompleter
{
    /// <summary>
    /// Gets the <see cref="System.Threading.Tasks.ValueTask"/> controlled by this instance.
    /// </summary>
    ValueTask ValueTask { get; }

    /// <summary>
    /// Set <see cref="ValueTaskSourceStatus.Succeeded"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Already completed.</exception>
    void SetResult();

    /// <summary>
    /// Set <see cref="ValueTaskSourceStatus.Faulted"/> or <see cref="ValueTaskSourceStatus.Canceled"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Already completed.</exception>
    void SetException(Exception exception);

    /// <summary>
    /// Try to set <see cref="ValueTaskSourceStatus.Succeeded"/>.
    /// </summary>
    bool TrySetResult();

    /// <summary>
    /// Try to set <see cref="ValueTaskSourceStatus.Faulted"/> or <see cref="ValueTaskSourceStatus.Canceled"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
    bool TrySetException(Exception exception);
}
