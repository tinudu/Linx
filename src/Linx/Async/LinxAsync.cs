using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Linx.Async;

/// <summary>
/// Static async methods.
/// </summary>
public static class LinxAsync
{
    /// <summary>
    /// Complete with or without an error.
    /// </summary>
    public static void SetExceptionOrResult(this IValueTaskCompleter completer, Exception? exception)
    {
        if (completer is null) throw new ArgumentNullException(nameof(completer));

        if (exception is null)
            completer.SetResult();
        else
            completer.SetException(exception);
    }

    /// <summary>
    /// Complete with an error or the specified result.
    /// </summary>
    public static void SetExceptionOrResult<T>(this IValueTaskCompleter<T> completer, Exception? exception, T result)
    {
        if (completer is null) throw new ArgumentNullException(nameof(completer));

        if (exception is null)
            completer.SetResult(result);
        else
            completer.SetException(exception);
    }

    /// <summary>
    /// Complete with or without an error.
    /// </summary>
    public static void SetExceptionOrResult(this in AsyncTaskMethodBuilder atmb, Exception? exception)
    {
        if (exception is null)
            atmb.SetResult();
        else
            atmb.SetException(exception);
    }

    /// <summary>
    /// Complete with an error or the specified result.
    /// </summary>
    public static void SetExceptionOrResult<T>(this in AsyncTaskMethodBuilder<T> atmb, Exception? exception, T result)
    {
        if (exception is null)
            atmb.SetResult(result);
        else
            atmb.SetException(exception);
    }

    /// <summary>
    /// Complete with or without an error.
    /// </summary>
    public static void SetExceptionOrResult(this TaskCompletionSource tcs, Exception? exception)
    {
        if (tcs is null) throw new ArgumentNullException(nameof(tcs));

        if (exception is null)
            tcs.SetResult();
        else
            tcs.SetException(exception);
    }

    /// <summary>
    /// Complete with an error or the specified result.
    /// </summary>
    public static void SetExceptionOrResult<T>(this TaskCompletionSource<T> ts, Exception? exception, T result)
    {
        if (ts is null) throw new ArgumentNullException(nameof(ts));

        if (exception is null)
            ts.SetResult(result);
        else
            ts.SetException(exception);
    }

    /// <summary>
    /// Gets a <see cref="ValueTask"/> that, when awaited, always schedules the continuation.
    /// </summary>
    /// <remarks>As opposed to <see cref="Task.Yield"/>, it can be configured to schedule the continuation on the current context.</remarks>
    public static ValueTask Yield() => YieldValueTaskSource.ValueTask;
}
