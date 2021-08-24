namespace Linx.Timing
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

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
        public ValueTask Delay(TimeSpan due, CancellationToken token) => new(due > TimeSpan.Zero ? Task.Delay(due, token) : Task.CompletedTask);

        /// <inheritdoc />
        public ValueTask Delay(DateTimeOffset due, CancellationToken token) => Delay(due - DateTimeOffset.Now, token);

        /// <inheritdoc />
        public ITimer GetTimer(CancellationToken token) => new Timer(token);

        private sealed class Timer : ITimer
        {
            private const int _sInitial = 0;
            private const int _sWaiting = 1;
            private const int _sCanceled = 2;
            private const int _sDisposed = 3;

            private readonly ManualResetValueTaskSource _ts = new();
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

            public ITime Time => RealTime.Instance;

            public ValueTask Delay(TimeSpan due)
            {
                _ts.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        if (due > TimeSpan.Zero)
                        {
                            _state = _sWaiting;
                            try { _timer.Change(due, Timeout.InfiniteTimeSpan); }
                            catch (Exception ex)
                            {
                                if (Atomic.CompareExchange(ref _state, _sInitial, _sWaiting) == _sWaiting)
                                    _ts.SetException(ex);
                            }
                        }
                        else
                        {
                            _state = _sInitial;
                            _ts.SetResult();
                        }

                        break;

                    case _sCanceled:
                        _state = _sCanceled;
                        _ts.SetException(new OperationCanceledException(_token));
                        break;

                    case _sDisposed:
                        _state = _sDisposed;
                        _ts.SetException(new ObjectDisposedException(nameof(ITimer)));
                        break;

                    default: // _sWaiting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _ts.Task;
            }

            public ValueTask Delay(DateTimeOffset due) => Delay(due - DateTimeOffset.Now);

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
                        _ts.SetException(new ObjectDisposedException(nameof(ITimer)));
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
                        _ts.SetException(new OperationCanceledException(_token));
                        break;

                    default: // Canceled, Disposed
                        _state = state;
                        break;
                }
            }

            private void TimerCallback(object _)
            {
                if (Atomic.CompareExchange(ref _state, _sInitial, _sWaiting) == _sWaiting)
                    _ts.SetResult();
            }
        }
    }
}