using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Async;

/// <summary>
/// Controls a manually resettable <see cref="ValueTask{T}"/>.
/// </summary>
[DebuggerNonUserCode]
public sealed class ManualResetValueTaskCompleter<T> : IValueTaskCompleter<T>, IValueTaskSource<T>, IValueTaskSource
{
    private ManualResetValueTaskCompleterCore<T> _core;

    /// <inheritdoc/>
    public ValueTask<T> ValueTask => new(this, _core.Version);

    /// <inheritdoc/>
    public ValueTask NonGenericValueTask => new(this, _core.Version);

    /// <summary>Resets to prepare for the next operation.</summary>
    public void Reset() => _core.Reset();

    /// <inheritdoc/>
    public void SetResult(T result) => _core.SetResult(result);

    /// <inheritdoc/>
    public bool TrySetResult(T result) => _core.TrySetResult(result);

    /// <inheritdoc/>
    public void SetException(Exception exception) => _core.SetException(exception);

    /// <inheritdoc/>
    public bool TrySetException(Exception exception) => _core.TrySetException(exception);

    ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => _core.GetStatus(token);
    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
    void IValueTaskSource<T>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    T IValueTaskSource<T>.GetResult(short token) => _core.GetResult(token);
    void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
}
