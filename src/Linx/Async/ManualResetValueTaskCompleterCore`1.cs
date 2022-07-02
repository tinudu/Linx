using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Async;

internal struct ManualResetValueTaskCompleterCore<TResult>
{
    // Re-implementation of ManualResetValueTaskSourceCore
    // because of concerns regarding race conditions between
    // Reset() and scheduled invocations of the continuation.

    private ValueTaskSourceStatus _status;
    private TResult? _result;
    private ExceptionDispatchInfo? _exception;
    private ExecutionContext? _executionContext;
    private Action<object?>? _continuation;
    private object? _continuationState;
    private int _version;

    public short Version => unchecked((short)Atomic.Read(in _version));

    private int LockValidate(short token)
    {
        var version = Atomic.Lock(ref _version);
        if (token != version)
        {
            _version = version;
            throw new InvalidOperationException();
        }
        return version;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        var version = LockValidate(token);
        var status = _status;
        _version = version;
        return status;
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        if (continuation is null) throw new ArgumentNullException(nameof(continuation));

        var version = LockValidate(token);
        if (_status == ValueTaskSourceStatus.Pending) // set _executionContext, _continuation, _continuationState
            try
            {
                if (_continuation is not null) // only one continuation supported
                    throw new InvalidOperationException();

                if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
                {
                    var sc = SynchronizationContext.Current;
                    if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                    {
                        var c = continuation;
                        continuation = s => sc.Post(new(c), s);
                    }
                    else
                    {
                        var ts = TaskScheduler.Current;
                        if (ts != TaskScheduler.Default)
                        {
                            var c = continuation;
                            continuation = s => Task.Factory.StartNew(c, s, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                        }
                    }
                }

                _executionContext = (flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0 ? ExecutionContext.Capture() : null;
                _continuation = continuation;
                _continuationState = state;
            }
            finally { _version = version; }
        else // completed, need to schedule
        {
            _version = version;

            if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
            {
                var sc = SynchronizationContext.Current;
                if (sc != null && sc.GetType() != typeof(SynchronizationContext))
                {
                    sc.Post(new(continuation), state);
                    return;
                }

                var ts = TaskScheduler.Current;
                if (ts != TaskScheduler.Default)
                {
                    Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                    return;
                }
            }

            var executionContext = (flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0 ? ExecutionContext.Capture() : null;
            if (executionContext is null)
                ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
            else
                ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
        }
    }

    public TResult GetResult(short token)
    {
        var version = LockValidate(token);
        if (_status == ValueTaskSourceStatus.Pending)
        {
            _version = version;
            throw new InvalidOperationException();
        }
        else
        {
            var exception = _exception;
            var result = _result;
            _version = version;
            exception?.Throw();
            return result!;
        }
    }

    public void Reset()
    {
        var version = Atomic.Lock(ref _version);
        _status = ValueTaskSourceStatus.Pending;
        _result = default;
        _exception = null;
        _executionContext = null;
        _continuation = null;
        _continuationState = null;
        _version = unchecked(++version);
    }

    public bool TrySetResult(TResult result)
    {
        Atomic.Lock(ref _version);
        if (_status != ValueTaskSourceStatus.Pending)
        {
            _version = ~_version;
            return false;
        }
        _result = result;
        _status = ValueTaskSourceStatus.Succeeded;
        SignalCompletion();
        return true;
    }

    public void SetResult(TResult result)
    {
        if (!TrySetResult(result))
            throw new InvalidOperationException();
    }

    public bool TrySetException(Exception exception)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));

        var dispatchInfo = ExceptionDispatchInfo.Capture(exception);
        var status = exception is OperationCanceledException ? ValueTaskSourceStatus.Canceled : ValueTaskSourceStatus.Faulted;

        Atomic.Lock(ref _version);
        if (_status != ValueTaskSourceStatus.Pending)
        {
            _version = ~_version;
            return false;
        }

        _exception = dispatchInfo;
        _status = status;
        SignalCompletion();
        return true;
    }

    public void SetException(Exception exception)
    {
        if (!TrySetException(exception))
            throw new InvalidOperationException();
    }

    private void SignalCompletion()
    {
        Debug.Assert(_version < 0 && _status != ValueTaskSourceStatus.Pending);

        if (_continuation is null)
        {
            _version = ~_version;
            return;
        }

        var continuation = Linx.Clear(ref _continuation);
        var continuationState = Linx.Clear(ref _continuationState);
        var executionContext = Linx.Clear(ref _executionContext);
        _version = ~_version;

        if (executionContext is null)
            continuation(continuationState);
        else
            ExecutionContext.Run(executionContext, new(continuation), continuationState);
    }
}
