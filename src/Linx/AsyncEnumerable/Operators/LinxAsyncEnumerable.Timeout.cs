namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Throws a <see cref="TimeoutException"/> if no element is observed within <paramref name="interval"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan interval)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

            return Create(token => new TimeoutEnumerator<T>(source, interval, token));
        }

        private sealed class TimeoutEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly IAsyncEnumerable<T> _source;
            private readonly TimeSpan _interval;
            private readonly ITime _time = Time.Current;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly ManualResetValueTaskSource<bool> _tsEmitting = new ManualResetValueTaskSource<bool>();
            private ManualResetValueTaskSource _tsTimeout;
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private int _state, _active;
            private Exception _error;
            private DateTimeOffset _due;

            public TimeoutEnumerator(IAsyncEnumerable<T> source, TimeSpan interval, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(interval > TimeSpan.Zero);
                _source = source;
                _interval = interval;
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
                        _due = _time.Now + _interval;
                        _active = 2;
                        _state = _sAccepting;
                        Produce();
                        TimeoutWatchdog();
                        break;

                    case _sEmitting:
                        _due = _time.Now + _interval;
                        var tsTimeout = Linx.Clear(ref _tsTimeout);
                        _state = _sAccepting;
                        _tsEmitting.SetResult(true);
                        tsTimeout?.SetResult();
                        break;

                    case _sCompleted:
                    case _sFinal:
                        Current = default;
                        _state = state;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting???
                        _state = state;
                        throw new Exception(state + "???");
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
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = error;
                        Current = default;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        var tsTimeout = Linx.Clear(ref _tsTimeout);
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _tsEmitting.SetResult(false);
                        tsTimeout?.SetResult();
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

            private void OnCompleted()
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
                    case _sAccepting:
                        Debug.Assert(_error == null);
                        Current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _tsAccepting.SetResult(false);
                        _atmbDisposed.SetResult();
                        break;

                    case _sEmitting:
                        Debug.Assert(_error == null);
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _atmbDisposed.SetResult();
                        break;

                    case _sCompleted:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default: // Initial, Final???
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private async void Produce()
            {
                try
                {
                    await foreach (var item in _source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                Current = item;
                                _tsEmitting.Reset();
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                if (await _tsEmitting.Task.ConfigureAwait(false)) continue;
                                return;

                            case _sCompleted:
                                _state = _sCompleted;
                                return;

                            default: // Initial, Emitting, Final???
                                _state = state;
                                throw new Exception(state + "???");
                        }
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }

            private async void TimeoutWatchdog()
            {
                try
                {
                    var ts = new ManualResetValueTaskSource();
                    using var timer = _time.GetTimer(_cts.Token);
                    while (true)
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (state)
                        {
                            case _sAccepting:
                                if (_time.Now < _due)
                                {
                                    var due = _due;
                                    _state = _sAccepting;
                                    await timer.Delay(due).ConfigureAwait(false);
                                    break;
                                }
                                else
                                {
                                    _state = _sAccepting;
                                    throw new TimeoutException();
                                }

                            case _sEmitting:
                                _tsTimeout = ts;
                                _state = _sEmitting;
                                await ts.Task.ConfigureAwait(false);
                                break;

                            case _sCompleted:
                                _state = _sCompleted;
                                return;

                            default: // Initial, Final???
                                _state = state;
                                throw new Exception(state + "???");
                        }
                    }
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }
        }
    }
}
