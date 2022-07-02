using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace Linx.Async;

internal sealed class YieldValueTaskSource : IValueTaskSource
{
    public static ValueTask ValueTask { get; } = new(new YieldValueTaskSource(), 0);

    private YieldValueTaskSource() { }

    public void GetResult(short token) { }

    public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Pending;

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        if (continuation is null) throw new ArgumentNullException(nameof(continuation));

        if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
        {
            var sc = SynchronizationContext.Current;
            if (sc != null && sc.GetType() != typeof(SynchronizationContext))
            {
                sc.Post(new SendOrPostCallback(continuation), state);
                return;
            }

            var ts = TaskScheduler.Current;
            if (ts != TaskScheduler.Default)
            {
                Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, ts);
                return;
            }
        }

        if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
            ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
        else
            ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
    }
}
