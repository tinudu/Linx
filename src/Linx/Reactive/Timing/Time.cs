namespace Linx.Reactive.Timing
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Access to current time.
    /// </summary>
    public static class Time
    {
        private static readonly AsyncLocal<ITime> _timeProvider = new AsyncLocal<ITime>();

        /// <summary>
        /// Gets the real time.
        /// </summary>
        public static ITime RealTime { get; } = new RealTimeImpl();

        /// <summary>
        /// Gets or sets the current time.
        /// </summary>
        public static ITime Current
        {
            get => _timeProvider.Value ?? RealTime;
            set => _timeProvider.Value = value ?? RealTime;
        }

        [DebuggerStepThrough]
        private sealed class RealTimeImpl : ITime
        {
            public DateTimeOffset Now => DateTimeOffset.Now;

            public async Task Wait(TimeSpan delay, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                if (delay > TimeSpan.Zero) await Task.Delay(delay, token).ConfigureAwait(false);
            }

            public async Task Wait(DateTimeOffset due, CancellationToken token) => await Wait(due - DateTimeOffset.Now, token).ConfigureAwait(false);

            public ITimer CreateTimer(TimerElapsedDelegte onElapsed) => new Timer(onElapsed);

            private sealed class Timer : ITimer
            {
                private const int _sDisabled = 0;
                private const int _sEnabled = 1;
                private const int _sDisposed = 2;
                private readonly TimerElapsedDelegte _onElapsed;
                private readonly System.Threading.Timer _timer;
                private int _state;
                private DateTimeOffset _due;

                public Timer(TimerElapsedDelegte onElapsed)
                {
                    _onElapsed = onElapsed ?? throw new ArgumentNullException(nameof(onElapsed));
                    _timer = new System.Threading.Timer(Callback);
                }

                public void Enable(TimeSpan delay) => Enable(delay, DateTimeOffset.Now + delay);

                public void Enable(DateTimeOffset due) => Enable(due - DateTimeOffset.Now, due);

                private void Enable(TimeSpan delay, DateTimeOffset due)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sDisabled:
                        case _sEnabled:
                            var prevDue = _due;
                            try
                            {
                                var millis = delay.Ticks / TimeSpan.TicksPerMillisecond;
                                if (millis > 0)
                                {
                                    if (!_timer.Change(millis, Timeout.Infinite))
                                        throw new Exception("Timer could not be changed.");
                                    _due = due;
                                    _state = _sEnabled;
                                }
                                else
                                {
                                    if (!_timer.Change(Timeout.Infinite, Timeout.Infinite))
                                        throw new Exception("Timer could not be changed.");
                                    _state = _sDisabled;
                                    try { _onElapsed(this, due); } catch { /**/ }
                                }
                            }
                            catch
                            {
                                _state = state;
                                _due = prevDue;
                                throw;
                            }
                            break;
                        default: // disposed
                            _state = state;
                            throw new ObjectDisposedException(nameof(ITimer));
                    }
                }

                public void Disable()
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sDisabled:
                            _state = _sDisabled;
                            break;
                        case _sEnabled:
                            try
                            {
                                if (!_timer.Change(Timeout.Infinite, Timeout.Infinite))
                                    throw new Exception("Timer could not be changed.");
                            }
                            catch
                            {
                                _state = _sEnabled;
                                throw;
                            }
                            _state = _sDisabled;
                            break;
                        default: // disposed
                            _state = state;
                            throw new ObjectDisposedException(nameof(ITimer));
                    }
                }

                private void Callback(object _)
                {
                    var state = Atomic.Lock(ref _state);
                    if (state != _sEnabled)
                    {
                        _state = state;
                        return;
                    }

                    var due = _due;
                    _state = _sDisabled;
                    try { _onElapsed(this, due); } catch { /**/ }
                }

                public void Dispose()
                {
                    var state = Atomic.Lock(ref _state);
                    if (state != _sDisposed) try { _timer.Dispose(); } catch { /**/ }
                    _state = _sDisposed;
                }
            }
        }

    }
}
