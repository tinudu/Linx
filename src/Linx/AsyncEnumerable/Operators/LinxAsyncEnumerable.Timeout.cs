using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasking;
using Linx.Timing;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Throws a <see cref="TimeoutException"/> if no element is observed within <paramref name="interval"/>.
    /// </summary>
    public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan interval, ITime time)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        return Create(token => new TimeoutEnumerator<T>(source, interval, time, token));
    }

    private sealed class TimeoutEnumerator<T> : IAsyncEnumerator<T>
    {
        private const int _sInitial = 0;
        private const int _sAccepting = 1;
        private const int _sEmitting = 2;
        private const int _sCompleted = 3;
        private const int _fProduce = 1 << 2;
        private const int _fTimer = 1 << 3;
        private const int _sFinal = _sCompleted | _fProduce | _fTimer;

        private readonly IAsyncEnumerable<T> _source;
        private readonly TimeSpan _interval;
        private readonly ITime _time;
        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
        private readonly ManualResetValueTaskSource<bool> _tsEmitting = new();
        private ManualResetValueTaskSource? _tsTimer;
        private AsyncTaskMethodBuilder _atmbDisposed = new();
        private int _state;
        private Exception? _error;
        private DateTimeOffset _due;

        public TimeoutEnumerator(IAsyncEnumerable<T> source, TimeSpan interval, ITime time, CancellationToken token)
        {
            Debug.Assert(source != null);
            Debug.Assert(interval > TimeSpan.Zero);

            _source = source;
            _interval = interval;
            _time = time ?? Time.RealTime;
            if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        public T Current { get; private set; } = default!;

        public ValueTask<bool> MoveNextAsync()
        {
            _tsAccepting.Reset();

            var state = Atomic.Lock(ref _state);
            switch (state & _sCompleted)
            {
                case _sInitial:
                    _due = _time.Now + _interval;
                    _state = _sAccepting;
                    Produce();
                    Timer();
                    break;

                case _sEmitting:
                    _due = _time.Now + _interval;
                    var tsTimeout = Linx.Clear(ref _tsTimer);
                    _state = _sAccepting;
                    _tsEmitting.SetResult(true);
                    tsTimeout?.SetResult();
                    break;

                case _sCompleted:
                    Current = default!;
                    _state = state;
                    _tsAccepting.SetExceptionOrResult(_error, false);
                    break;

                default: // Accepting???
                    _state = state;
                    throw new Exception(state + "???");
            }

            return _tsAccepting.ValueTask;
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
            switch (state & _sCompleted)
            {
                case _sInitial:
                    _error = error;
                    _state = _sFinal;
                    _ctr.Dispose();
                    _atmbDisposed.SetResult();
                    break;

                case _sAccepting:
                    _error = error;
                    Current = default!;
                    _state = _sCompleted;
                    _ctr.Dispose();
                    _cts.TryCancel();
                    _tsAccepting.SetException(error);
                    break;

                case _sEmitting:
                    _error = error;
                    var tsTimeout = Linx.Clear(ref _tsTimer);
                    _state = _sCompleted;
                    _ctr.Dispose();
                    _cts.TryCancel();
                    _tsEmitting.SetResult(false);
                    tsTimeout?.SetResult();
                    break;

                case _sCompleted:
                    _state = state;
                    break;

                default:
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
                    switch (state & _sCompleted)
                    {
                        case _sAccepting:
                            Current = item;
                            _tsEmitting.Reset();
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                            if (!await _tsEmitting.ValueTask.ConfigureAwait(false)) return;
                            break;

                        case _sCompleted:
                            _state = state;
                            return;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
            catch (Exception ex) { OnError(ex); }
            finally
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        Debug.Assert(_error == null);
                        Current = default!;
                        _state = _sCompleted | _fProduce;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        _tsAccepting.SetResult(false);
                        break;

                    case _sCompleted:
                        _state = _sCompleted | _fProduce;
                        break;

                    case _sCompleted | _fTimer:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default: // Initial, Emitting???
                        _state = state | _fProduce;
                        break;
                }
            }
        }

        private async void Timer()
        {
            try
            {
                var ts = new ManualResetValueTaskSource();
                using var timer = _time.GetTimer(_cts.Token);
                while (true)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state & _sCompleted)
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
                            _tsTimer = ts;
                            _state = _sEmitting;
                            await ts.ValueTask.ConfigureAwait(false);
                            break;

                        case _sCompleted:
                            _state = state;
                            return;

                        default: // Initial???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
            catch (Exception ex) { OnError(ex); }
            finally
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sCompleted:
                        _state = _sCompleted | _fTimer;
                        break;

                    case _sCompleted | _fProduce:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default:
                        _state = state | _fTimer;
                        break;
                }
            }
        }
    }
}
