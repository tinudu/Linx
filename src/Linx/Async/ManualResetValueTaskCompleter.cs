using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Async;

/// <summary>
/// Controls a manually resettable <see cref="System.Threading.Tasks.ValueTask"/>.
/// </summary>
[DebuggerNonUserCode]
public sealed class ManualResetValueTaskCompleter : IValueTaskCompleter, IValueTaskSource
{
    private ManualResetValueTaskSourceCore<Unit> _core;

    /// <inheritdoc/>
    public ValueTask ValueTask => new(this, _core.Version);

    /// <summary>Resets to prepare for the next operation.</summary>
    public void Reset() => _core.Reset();

    /// <inheritdoc/>
    public void SetResult() => _core.SetResult(default);

    /// <inheritdoc/>
    public void SetException(Exception exception) => _core.SetException(exception);

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _core.GetStatus(token);
    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _core.OnCompleted(continuation, state, token, flags);
    void IValueTaskSource.GetResult(short token) => _core.GetResult(token);
}
