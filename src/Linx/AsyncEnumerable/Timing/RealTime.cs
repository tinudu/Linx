namespace Linx.AsyncEnumerable.Timing
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    /// <summary>
    /// The real time.
    /// </summary>
    [DebuggerStepThrough]
    public sealed class RealTime : ITime
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static RealTime Instance { get; } = new RealTime();

        private RealTime() { }

        /// <inheritdoc />
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <inheritdoc />
        public Task Delay(TimeSpan delay, CancellationToken token) => delay > TimeSpan.Zero ? Task.Delay(delay, token) : Task.CompletedTask;

        /// <inheritdoc />
        public Task Delay(DateTimeOffset due, CancellationToken token) => Delay(due - DateTimeOffset.Now, token);

        /// <inheritdoc />
        public ITimer GetTimer(CancellationToken token) => new Timer(token);

        private sealed class Timer : ITimer
        {
            private const int _sInitial = 0;
            private const int _sWaiting = 1;
            private const int _sCanceled = 2;
            private const int _sDisposed = 3;

            private readonly ManualResetValueTaskSource _tp = new ManualResetValueTaskSource();
            private readonly System.Threading.Timer _timer;
            private readonly CancellationToken _token;
            private CancellationTokenRegistration _ctr;
            private int _state;

            public Timer(CancellationToken token)
            {
                _timer = new System.Threading.Timer(TimerCallback);
                _token = token;
                if (_token.CanBeCanceled) _ctr = token.Register(TokenCanceled);
            }

            public ValueTask Delay(DateTimeOffset due) => Delay(due - DateTimeOffset.Now);

            public ValueTask Delay(TimeSpan due)
            {
                _tp.Reset();
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        var millis = due.Ticks / TimeSpan.TicksPerMillisecond;
                        if (millis <= 0)
                        {
                            _state = _sInitial;
                            _tp.SetResult();
                        }
                        else
                        {
                            _state = _sWaiting;
                            try { _timer.Change(millis, Timeout.Infinite); }
                            catch (Exception ex)
                            {
                                if (Atomic.TestAndSet(ref _state, _sWaiting, _sInitial) == _sWaiting)
                                    _tp.SetException(ex);
                            }
                        }

                        break;
                    case _sCanceled:
                        _state = _sCanceled;
                        _tp.SetException(new OperationCanceledException(_token));
                        break;
                    case _sDisposed:
                        _state = _sDisposed;
                        _tp.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;
                    default: // _sWaiting???
                        _state = state;
                        _tp.SetException(new InvalidOperationException());
                        break;
                }

                return _tp.Task;
            }

            public void Elapse()
            {
                if (Atomic.TestAndSet(ref _state, _sWaiting, _sInitial) != _sWaiting) return;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                _tp.SetResult();
            }

            public void Dispose()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _timer.Dispose();
                        break;
                    case _sWaiting:
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _timer.Dispose();
                        _tp.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;
                    default: // Canceled, Disposed
                        _state = _sDisposed;
                        break;
                }
            }

            private void TokenCanceled()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sCanceled;
                        _ctr.Dispose();
                        _timer.Dispose();
                        break;
                    case _sWaiting:
                        _state = _sCanceled;
                        _ctr.Dispose();
                        _timer.Dispose();
                        _tp.SetException(new OperationCanceledException(_token));
                        break;
                    default: // Canceled, Disposed
                        _state = state;
                        break;
                }
            }

            private void TimerCallback(object _)
            {
                if (Atomic.TestAndSet(ref _state, _sWaiting, _sInitial) == _sWaiting)
                    _tp.SetResult();
            }
        }
    }
}