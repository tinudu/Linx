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
        /// Ignores values which are followed by another value before <paramref name="interval"/>.
        /// </summary>
        public static ILinxObservable<T> Throttle<T>(this ILinxObservable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return interval > TimeSpan.Zero ? new ThrottleObservable<T>(source, interval) : source;
        }

        private sealed class ThrottleObservable<T> : ILinxObservable<T>
        {
            private readonly ILinxObservable<T> _source;
            private readonly TimeSpan _interval;

            public ThrottleObservable(ILinxObservable<T> source, TimeSpan interval)
            {
                _source = source;
                _interval = interval;
            }

            public void Subscribe(ILinxObserver<T> observer)
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                try { _source.Subscribe(new Observer(_interval, observer)); }
                catch (Exception ex) { observer.OnError(ex); }
            }

            public override string ToString() => "Throttle";

            private sealed class Observer : ILinxObserver<T>
            {
                private const int _sInitial = 0;
                private const int _sNext = 1;
                private const int _sCanceling = 2;
                private const int _sFinal = 3;

                private readonly TimeSpan _interval;
                private readonly ILinxObserver<T> _observer;

                private readonly ITime _time = Time.Current;
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private int _active = 2; // this and Throttle()
                private int _state;
                private Exception _error;
                private T _next;
                private DateTimeOffset _due;
                private ManualResetValueTaskSource _tsThrottleIdle;

                public Observer(TimeSpan interval, ILinxObserver<T> observer)
                {
                    _interval = interval;
                    _observer = observer;
                    var token = observer.Token;
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                    Throttle();
                }

                CancellationToken ILinxObserver<T>.Token => _cts.Token;

                bool ILinxObserver<T>.OnNext(T value)
                {
                    _cts.Token.ThrowIfCancellationRequested();

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

                void ILinxObserver<T>.OnError(Exception error)
                {
                    Cancel(error ?? new ArgumentNullException(nameof(error)));
                    Complete();
                }

                void ILinxObserver<T>.OnCompleted() => Complete();

                private async void Throttle()
                {
                    try
                    {
                        var idle = new ManualResetValueTaskSource();

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
                                    break;

                                case _sNext:
                                    if (_due <= _time.Now)
                                    {
                                        var value = _next;
                                        _state = _sInitial;
                                        if (!_observer.OnNext(value))
                                        {
                                            Cancel(null);
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        var due = _due;
                                        _state = _sNext;
                                        await _time.Delay(due, _cts.Token).ConfigureAwait(false);
                                    }
                                    break;

                                case _sCanceling:
                                    _state = state;
                                    return;

                                default: // completed???
                                    _state = state;
                                    throw new Exception(state + "???");
                            }
                        }
                    }
                    catch (Exception ex) { Cancel(ex); }
                    finally { Complete(); }
                }

                private void SetError(Exception errorOpt)
                {
                    if (errorOpt != null && (_error == null || _error is OperationCanceledException oce && oce.CancellationToken == _cts.Token))
                        _error = errorOpt;
                }

                private void Cancel(Exception errorOpt)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sNext:
                            SetError(errorOpt);
                            _next = default;
                            var idle = Linx.Clear(ref _tsThrottleIdle);
                            _state = _sCanceling;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            idle?.SetResult();
                            break;

                        case _sCanceling:
                            SetError(errorOpt);
                            _state = _sCanceling;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void Complete()
                {
                    var state = Atomic.Lock(ref _state);

                    Debug.Assert(_active > 0);
                    if (--_active > 0)
                    {
                        _state = state;
                        return;
                    }

                    switch (state)
                    {
                        case _sInitial:
                        case _sNext:
                            _next = default;
                            var idle = Linx.Clear(ref _tsThrottleIdle);
                            _state = _sFinal;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            idle?.SetResult();
                            break;

                        case _sCanceling:
                            _state = _sFinal;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            return;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    if (_error == null) _observer.OnCompleted();
                    else _observer.OnError(_error);
                }
            }
        }
    }
}
