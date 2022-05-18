using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using global::Linx.Tasks;

namespace Linx.Timing;

/// <summary>
/// Access to current time.
/// </summary>
public static class Time
{
    /// <summary>
    /// Gets the real <see cref="ITime"/>.
    /// </summary>
    public static ITime RealTime { get; } = new RealTimeImpl();

    /// <summary>
    /// Schedule an action.
    /// </summary>
    public static async void Schedule(this ITime time, Action action, DateTimeOffset due, CancellationToken token)
    {
        if (time == null) throw new ArgumentNullException(nameof(time));
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (token.IsCancellationRequested) return;

        try
        {
            await time.Delay(due, token).ConfigureAwait(false);
            action();
        }
        catch {/**/}
    }

    /// <summary>
    /// Schedule an action.
    /// </summary>
    public static async void Schedule(this ITime time, Action action, TimeSpan due, CancellationToken token)
    {
        if (time == null) throw new ArgumentNullException(nameof(time));
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (token.IsCancellationRequested) return;

        try
        {
            await time.Delay(due, token).ConfigureAwait(false);
            action();
        }
        catch {/**/}
    }

    [DebuggerStepThrough]
    private sealed class RealTimeImpl : ITime
    {
        /// <inheritdoc />
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <inheritdoc />
        public ValueTask Delay(TimeSpan due, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new(due > TimeSpan.Zero ? Task.Delay(due, token) : Task.CompletedTask);
        }

        /// <inheritdoc />
        public ValueTask Delay(DateTimeOffset due, CancellationToken token) => Delay(due - DateTimeOffset.Now, token);

        /// <inheritdoc />
        public ITimer GetTimer(CancellationToken token) => new Timer(token);

        private sealed class Timer : ITimer
        {
            private const int _sInitial = 0;
            private const int _sWaiting = 1;
            private const int _sFinal = 2;

            private static readonly ObjectDisposedException _timerDisposedException = new(nameof(ITimer));

            private readonly ManualResetValueTaskSource _tsDelay = new();
            private readonly System.Threading.Timer _timer;
            private readonly CancellationTokenRegistration _ctr;
            private int _state;

            public Timer(CancellationToken token)
            {
                _timer = new System.Threading.Timer(TimerCallback);
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
            }

            public ValueTask Delay(TimeSpan due)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _tsDelay.Reset();
                        if (due > TimeSpan.Zero)
                        {
                            _state = _sWaiting;
                            try { _timer.Change(due, Timeout.InfiniteTimeSpan); }
                            catch (Exception ex)
                            {
                                if (Atomic.CompareExchange(ref _state, _sInitial, _sWaiting) == _sWaiting)
                                    _tsDelay.SetException(ex);
                            }
                        }
                        else
                        {
                            _state = _sInitial;
                            _tsDelay.SetResult();
                        }
                        return _tsDelay.Task;

                    case _sFinal:
                        _state = _sFinal;
                        return _tsDelay.Task;

                    case _sWaiting:
                        _state = _sWaiting;
                        throw new InvalidOperationException(Strings.MethodIsNotReentrant);

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public ValueTask Delay(DateTimeOffset due) => Delay(due - DateTimeOffset.Now);

            public void Dispose() => SetFinal(_timerDisposedException);

            private void SetFinal(Exception error)
            {
                Debug.Assert(error is not null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _tsDelay.Reset();
                        _state = _sFinal;
                        _ctr.Dispose();
                        _tsDelay.SetException(error);
                        break;

                    case _sWaiting:
                        _state = _sFinal;
                        _ctr.Dispose();
                        _tsDelay.SetException(error);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    default:
                        _state = state;
                        throw new Exception(_state + "???");
                }
            }

            private void TimerCallback(object? _)
            {
                if (Atomic.CompareExchange(ref _state, _sInitial, _sWaiting) == _sWaiting)
                    _tsDelay.SetResult();
            }
        }
    }

}
