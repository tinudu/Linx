namespace Linx.Observable
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using TaskSources;
    using Timing;

    partial class LinxObservable
    {
        /// <summary>
        /// Ignores values which are followed by another value before the specified interval in milliseconds.
        /// </summary>
        public static ILinxObservable<T> Throttle<T>(this ILinxObservable<T> source, int intervalMilliseconds)
            => source.Throttle(TimeSpan.FromTicks(intervalMilliseconds * TimeSpan.TicksPerMillisecond));

        /// <summary>
        /// Ignores values which are followed by another value within the specified interval.
        /// </summary>
        public static ILinxObservable<T> Throttle<T>(this ILinxObservable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (interval <= TimeSpan.Zero) return source;

            return Create<T>(observer =>
            {
                var throttleObserver = new ThrottleObserver<T>(interval, observer);
                try { source.Subscribe(throttleObserver); }
                catch (Exception ex) { throttleObserver.OnError(ex); }
            });
        }

        private sealed class ThrottleObserver<T> : ILinxObserver<T>
        {
            private const int _sInitial = 0;
            private const int _sNext = 1;
            private const int _sLast = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly ITime _time = Time.Current;
            private readonly ILinxObserver<T> _observer;
            private readonly TimeSpan _interval;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private ManualResetValueTaskSource _tsThrottleIdle;
            private int _active = 2; // (ILinxObserver<T>)this and Throttle()
            private int _state;
            private T _next;
            private DateTimeOffset _due;
            private Exception _error;

            public ThrottleObserver(TimeSpan interval, ILinxObserver<T> observer)
            {
                _interval = interval;
                _observer = observer;
                var token = observer.Token;
                if (token.CanBeCanceled) _ctr = token.Register(() => SetCompleted(new OperationCanceledException(token)));
                Throttle();
            }

            private void SetCompleted(Exception errorOpt)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                    case _sNext:
                    case _sLast:
                        Debug.Assert(_active > 0);
                        _error = errorOpt;
                        _next = default;
                        var idle = Linx.Clear(ref _tsThrottleIdle);
                        _state = _sCompleted;
                        _ctr.Dispose();
                        try { _cts.Cancel(); } catch { /**/ }
                        idle?.SetResult();
                        break;

                    case _sCompleted:
                    case _sFinal:
                        _state = state;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private void SetFinal()
            {
                var state = Atomic.Lock(ref _state);
                Debug.Assert(_active > 0 && state == _sCompleted);
                if (--_active > 0)
                    _state = state;
                else
                {
                    _state = _sFinal;
                    if (_error == null) _observer.OnCompleted();
                    else _observer.OnError(_error);
                }
            }

            CancellationToken ILinxObserver<T>.Token => _cts.Token;

            bool ILinxObserver<T>.OnNext(T value)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                    case _sNext:
                        _next = value;
                        _due = _time.Now + _interval;
                        var idle = Linx.Clear(ref _tsThrottleIdle);
                        _state = _sNext;
                        idle?.SetResult();
                        return true;

                    default:
                        _state = state;
                        return false;
                }
            }

            public void OnError(Exception error)
            {
                SetCompleted(error ?? new ArgumentNullException(nameof(error)));
                SetFinal();
            }

            void ILinxObserver<T>.OnCompleted()
            {
                SetCompleted(null);
                SetFinal();
            }

            private async void Throttle()
            {
                Exception error = null;
                try
                {
                    var idle = new ManualResetValueTaskSource();
                    using var timer = _time.GetTimer(_cts.Token);
                    while (true)
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sInitial:
                                idle.Reset();
                                _tsThrottleIdle = idle;
                                _state = _sInitial;
                                await idle.Task.ConfigureAwait(false);
                                continue;

                            case _sNext:
                            case _sLast:
                                if (_due > _time.Now)
                                {
                                    var due = _due;
                                    _state = state;
                                    await timer.Delay(due).ConfigureAwait(false);
                                    continue;
                                }

                                if (state == _sNext)
                                {
                                    var value = _next;
                                    _state = _sInitial;
                                    if (_observer.OnNext(value)) continue;
                                }
                                else // Last
                                {
                                    var value = _next;
                                    _state = _sLast; // temporarily
                                    _observer.OnNext(value);
                                }
                                return;

                            case _sCompleted:
                                _state = state;
                                return;

                            //case _sFinal:
                            default:
                                _state = state;
                                throw new Exception(state + "???");
                        }
                    }
                }
                catch (Exception ex) { error = ex; }
                finally
                {
                    SetCompleted(error);
                    SetFinal();
                }
            }
        }
    }
}
