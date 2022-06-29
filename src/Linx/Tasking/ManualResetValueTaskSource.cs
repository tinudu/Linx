using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Tasking;

/// <summary>
/// Controls a manually resettable <see cref="System.Threading.Tasks.ValueTask"/>.
/// </summary>
[DebuggerNonUserCode]
public sealed class ManualResetValueTaskSource : ILinxValueTaskSource
{
    private ManualResetValueTaskSourceCore<Unit> _core;

    /// <inheritdoc/>
    public ValueTask ValueTask => new(this, _core.Version);

    /// <summary>
    /// Gets or sets whether to force continuations to run asynchronously.
    /// </summary>
    /// <value>true to force continuations to run asynchronously; otherwise, false.</value>
    public bool RunContinuationsAsynchronously
    {
        get => _core.RunContinuationsAsynchronously;
        set => _core.RunContinuationsAsynchronously = value;
    }

    /// <summary>Resets to prepare for the next operation.</summary>
    public void Reset() => _core.Reset();

    /// <inheritdoc/>
    public void SetResult() => _core.SetResult(default);

    /// <inheritdoc/>
    public void SetException(Exception exception) => _core.SetException(exception);

    /// <inheritdoc/>
    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    /// <inheritdoc/>
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);

    /// <inheritdoc/>
    public void GetResult(short token) => _core.GetResult(token);
}

/// <summary>
/// Controls a manually resettable <see cref="ValueTask{T}"/>.
/// </summary>
[DebuggerNonUserCode]
public sealed class ManualResetValueTaskSource<T> : ILinxValueTaskSource<T>, IValueTaskSource
{
    private ManualResetValueTaskSourceCore<T> _core;

    /// <inheritdoc/>
    public ValueTask<T> ValueTask => new(this, _core.Version);

    /// <inheritdoc/>
    public ValueTask ValueTaskNonGeneric => new(this, _core.Version);

    /// <summary>
    /// Gets or sets whether to force continuations to run asynchronously.
    /// </summary>
    /// <value>true to force continuations to run asynchronously; otherwise, false.</value>
    public bool RunContinuationsAsynchronously
    {
        get => _core.RunContinuationsAsynchronously;
        set => _core.RunContinuationsAsynchronously = value;
    }

    /// <summary>Resets to prepare for the next operation.</summary>
    public void Reset() => _core.Reset();

    /// <inheritdoc/>
    public void SetResult(T result) => _core.SetResult(result);

    /// <inheritdoc/>
    public void SetException(Exception exception) => _core.SetException(exception);

    /// <inheritdoc/>
    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

    /// <inheritdoc/>
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);

    /// <inheritdoc/>
    public T GetResult(short token) => _core.GetResult(token);

    void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
}
