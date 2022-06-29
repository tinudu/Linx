using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Tasking;

/// <summary>
/// Controls a <see cref="ValueTask"/>.
/// </summary>
public interface ILinxValueTaskSource : IValueTaskSource
{
    /// <summary>
    /// Gets a <see cref="ValueTask"/> from this instance.
    /// </summary>
    ValueTask ValueTask { get; }

    /// <summary>Completes successfully.</summary>
    void SetResult();

    /// <summary>Completes with an error.</summary>
    void SetException(Exception exception);
}

/// <summary>
/// Controls a <see cref="ValueTask{T}"/>.
/// </summary>
public interface ILinxValueTaskSource<T> : IValueTaskSource<T>
{
    /// <summary>
    /// Gets a <see cref="ValueTask{T}"/> from this instance.
    /// </summary>
    ValueTask<T> ValueTask { get; }

    /// <summary>
    /// Gets a <see cref="ValueTask"/> from this instance.
    /// </summary>
    ValueTask ValueTaskNonGeneric { get; }

    /// <summary>Completes successfully.</summary>
    void SetResult(T result);

    /// <summary>Completes with an error.</summary>
    void SetException(Exception exception);
}
