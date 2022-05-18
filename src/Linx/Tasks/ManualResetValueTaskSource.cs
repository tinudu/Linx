// NOTE: expected to exist similarly in future framework/core versions, when it will be replaced.
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Tasks;

/// <summary>
/// Controls a manually resettable <see cref="ValueTask"/>.
/// </summary>
[DebuggerNonUserCode]
public sealed class ManualResetValueTaskSource : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<Unit> _core = new();

    /// <summary>
    /// Gets a <see cref="ValueTask"/>.
    /// </summary>
    public ValueTask Task => new(this, _core.Version);

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

    /// <summary>Completes successfully.</summary>
    public void SetResult() => _core.SetResult(default);

    /// <summary>Completes with an error.</summary>
    public void SetException(Exception exception) => _core.SetException(exception);

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
}

/// <summary>
/// Controls a manually resettable <see cref="ValueTask{T}"/>.
/// </summary>
[DebuggerNonUserCode]
public sealed class ManualResetValueTaskSource<T> : IValueTaskSource<T>, IValueTaskSource
{
    private ManualResetValueTaskSourceCore<T> _core = new();

    /// <summary>
    /// Gets a <see cref="ValueTask{TResult}"/>.
    /// </summary>
    public ValueTask<T> Task => new(this, _core.Version);

    /// <summary>
    /// Gets a <see cref="ValueTask"/>.
    /// </summary>
    public ValueTask TaskNonGeneric => new(this, _core.Version);

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

    /// <summary>Completes successfully.</summary>
    public void SetResult(T result) => _core.SetResult(result);

    /// <summary>Completes with an error.</summary>
    public void SetException(Exception exception) => _core.SetException(exception);

    ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => _core.GetStatus(token);
    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);
    void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
}