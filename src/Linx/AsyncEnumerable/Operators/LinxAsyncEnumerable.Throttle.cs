namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores values which are followed by another value before the specified interval in milliseconds.
        /// </summary>
        public static IAsyncEnumerable<T> Throttle<T>(this IAsyncEnumerable<T> source, int intervalMilliseconds)
            => source.Throttle(TimeSpan.FromMilliseconds(intervalMilliseconds));

        /// <summary>
        /// Ignores values which are followed by another value within the specified interval.
        /// </summary>
        public static IAsyncEnumerable<T> Throttle<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return interval <= TimeSpan.Zero ?
                source :
                Create(token => new ThrottleEnumerator<T>(source, interval, token));
        }

        private sealed class ThrottleEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sNextAccepting = 3;
            private const int _sNextEmitting = 4;
            private const int _sLast = 5;
            private const int _sFinal = 6;

            private readonly IAsyncEnumerable<T> _source;
            private readonly TimeSpan _interval;
            private readonly ITime _time;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private ManualResetValueTaskSource<bool> _tsThrottle;
            private int _state, _active;
            private Exception _error;
            private T _next;
            private DateTimeOffset _due;

            public ThrottleEnumerator(IAsyncEnumerable<T> source, TimeSpan interval, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(interval > TimeSpan.Zero);

                _source = source;
                _interval = interval;
                _time = Time.Current;

                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsAccepting.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _active = 2;
                        _state = _sAccepting;
                        Produce();
                        Throttle();
                        break;

                    case _sEmitting:
                        _state = _sAccepting;
                        break;

                    case _sNextEmitting:
                        if (_due <= _time.Now)
                        {
                            Current = _next;
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        else
                            _state = _sNextAccepting;
                        break;

                    case _sLast:
                        Current = Linx.Clear(ref _next);
                        _state = _sFinal;
                        _ctr.Dispose();
                        _tsAccepting.SetResult(true);
                        break;

                    default:
                        Debug.Assert(state == _sFinal);
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                OnError(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void OnError(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                var tsThrottle = Linx.Clear(ref _tsThrottle);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                    case _sNextAccepting:
                        Current = _next = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        tsThrottle?.SetResult(false);
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                    case _sNextEmitting:
                        _next = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        tsThrottle?.SetResult(false);
                        break;

                    case _sLast:
                        _next = default;
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        break;

                    default:
                        Debug.Assert(state == _sFinal);
                        _state = _sFinal;
                        break;
                }
            }

            private async void Produce()
            {
                try
                {
                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var due = _time.Now + _interval;

                        var state = Atomic.Lock(ref _state);
                        var ts = Linx.Clear(ref _tsThrottle);
                        switch (state)
                        {
                            case _sAccepting:
                            case _sNextAccepting:
                                _next = item;
                                _due = due;
                                _state = _sNextAccepting;
                                ts?.SetResult(true);
                                break;

                            case _sEmitting:
                            case _sNextEmitting:
                                _next = item;
                                _due = due;
                                _state = _sNextEmitting;
                                ts?.SetResult(true);
                                break;

                            default:
                                Debug.Assert(state == _sFinal && _error != null && ts == null);
                                _state = _sFinal;
                                return;
                        }
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally
                {
                    var state = Atomic.Lock(ref _state);
                    var ts = Linx.Clear(ref _tsThrottle);
                    switch (state)
                    {
                        case _sAccepting:
                            Current = _next = default;
                            _state = _sFinal;
                            _ctr.Dispose();
                            ts?.SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
                            break;

                        case _sNextAccepting:
                            Current = Linx.Clear(ref _next);
                            _state = _sFinal;
                            _ctr.Dispose();
                            ts?.SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(true);
                            break;

                        case _sEmitting:
                            _state = _sFinal;
                            _ctr.Dispose();
                            ts?.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sNextEmitting:
                            _state = _sLast;
                            ts?.SetResult(false);
                            _cts.TryCancel();
                            break;

                        default:
                            Debug.Assert(state == _sFinal && _error != null && ts == null);
                            _state = _sFinal;
                            break;
                    }

                    if (Interlocked.Decrement(ref _active) == 0)
                        _atmbDisposed.SetResult();
                }
            }

            private async void Throttle()
            {
                try
                {
                    var ts = new ManualResetValueTaskSource<bool>();
                    using var timer = _time.GetTimer(_cts.Token);
                    while (true)
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                            case _sEmitting:
                                ts.Reset();
                                _tsThrottle = ts;
                                _state = state;
                                if (!await ts.Task.ConfigureAwait(false))
                                    return;
                                break;

                            case _sNextAccepting:
                                if (_due <= _time.Now)
                                {
                                    Current = _next;
                                    _state = _sEmitting;
                                    _tsAccepting.SetResult(true);
                                }
                                else
                                {
                                    var due = _due;
                                    _state = _sNextAccepting;
                                    await timer.Delay(due).ConfigureAwait(false);
                                }
                                break;

                            case _sNextEmitting:
                                if (_due <= _time.Now)
                                {
                                    ts.Reset();
                                    _tsThrottle = ts;
                                    _state = _sNextEmitting;
                                    if (!await ts.Task.ConfigureAwait(false))
                                        return;
                                }
                                else
                                {
                                    var due = _due;
                                    _state = _sNextEmitting;
                                    await timer.Delay(due).ConfigureAwait(false);
                                }
                                break;

                            default:
                                Debug.Assert(state == _sLast || state == _sFinal);
                                _state = state;
                                return;
                        }
                    }
                }
                catch (OperationCanceledException oce) when (oce.CancellationToken == _cts.Token) { }
                catch (Exception ex) { OnError(ex); }
                finally
                {
                    if (Interlocked.Decrement(ref _active) == 0)
                        _atmbDisposed.SetResult();
                }
            }
        }
    }
}
