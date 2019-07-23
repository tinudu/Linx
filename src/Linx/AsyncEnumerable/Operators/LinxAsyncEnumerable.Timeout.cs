namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Throws a <see cref="TimeoutException"/> if no element is observed within <paramref name="dueTime"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Timeout<T>(this IAsyncEnumerable<T> source, TimeSpan dueTime)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (dueTime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(dueTime));

            return Generate<T>(async (yield, token) =>
            {
                // ReSharper disable InconsistentNaming
                const int _sInitial = 0; // MoveNextAsync returned within time
                const int _sAccepting = 1; // pending MoveNextAsync - due time in _due
                const int _sCanceling = 2; // canceling because of error or because completed

                var _time = Time.Current;
                var _cts = new CancellationTokenSource();
                CancellationTokenRegistration _ctr = default;
                var _state = _sInitial;
                DateTimeOffset _due = default;
                Exception _error = null;
                ManualResetValueTaskSource _tsWatchdogIdle = null;
                // ReSharper restore InconsistentNaming

                if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));

                var tTimerWatchdog = TimerWatchdog();

                Exception err = null;
                try
                {
                    var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (true)
                        {
                            // set accepting
                            var state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sInitial:
                                    var idle = Linx.Clear(ref _tsWatchdogIdle);
                                    _due = _time.Now + dueTime;
                                    _state = _sAccepting;
                                    idle?.SetResult();
                                    break;

                                case _sCanceling:
                                    _state = _sCanceling;
                                    return;

                                default: // accepting???
                                    _state = state;
                                    throw new Exception(state + "???");
                            }

                            if(!await ae.MoveNextAsync()) return;

                            // set initial
                            state = Atomic.Lock(ref _state);
                            switch (state)
                            {
                                case _sAccepting:
                                    _state = _sInitial;
                                    break;

                                case _sCanceling:
                                    _state = _sCanceling;
                                    return;

                                default: // initial???
                                    _state = state;
                                    throw new Exception(state + "???");
                            }

                            if(!await yield(ae.Current)) return;
                        }
                    }
                    finally { await ae.DisposeAsync(); }
                }
                catch (Exception ex) { err = ex; }
                finally
                {
                    Cancel(err);
                    await tTimerWatchdog.ConfigureAwait(false);
                    if (_error != null) throw _error;
                }

                void Cancel(Exception error)
                {
                    if (error is OperationCanceledException oce && oce.CancellationToken == _cts.Token)
                        error = null;

                    // ReSharper disable AccessToModifiedClosure
                    var state = Atomic.Lock(ref _state);
                    var idle = Linx.Clear(ref _tsWatchdogIdle);
                    switch (state)
                    {
                        case _sInitial:
                        case _sAccepting:
                            _error = error;
                            _state = _sCanceling;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            break;

                        case _sCanceling:
                            if (_error == null) _error = error;
                            _state = _sCanceling;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    idle?.SetResult();
                    // ReSharper restore AccessToModifiedClosure
                }

                async Task TimerWatchdog()
                {
                    // ReSharper disable AccessToModifiedClosure
                    try
                    {
                        var idle = new ManualResetValueTaskSource();

                        using (var timer = _time.GetTimer(_cts.Token))
                            while (true)
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sInitial: // no time to wait for
                                        idle.Reset();
                                        _tsWatchdogIdle = idle;
                                        _state = _sInitial;
                                        await _tsWatchdogIdle.Task.ConfigureAwait(false);
                                        break;

                                    case _sAccepting:
                                        if (_time.Now < _due)
                                        {
                                            var due = _due;
                                            _state = _sAccepting;
                                            await timer.Delay(due).ConfigureAwait(false);
                                        }
                                        else
                                        {
                                            _state = _sAccepting;
                                            throw new TimeoutException();
                                        }

                                        break;

                                    case _sCanceling:
                                        _state = _sCanceling;
                                        return;

                                    default:
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                    }
                    catch (Exception ex) { Cancel(ex); }
                    // ReSharper restore AccessToModifiedClosure
                }
            });
        }
    }
}
