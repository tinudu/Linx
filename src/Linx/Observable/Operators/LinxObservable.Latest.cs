namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncEnumerable;
    using TaskSources;

    partial class LinxObservable
    {
        /// <summary>
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LatestOneEnumerable<T>(source);
        }

        private sealed class LatestOneEnumerable<T> : IAsyncEnumerable<T>, ILinxObservable<T>
        {
            private readonly ILinxObservable<T> _source;

            public LatestOneEnumerable(ILinxObservable<T> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
                => new Enumerator(this, token);

            public override string ToString() => "Latest";

            public void Subscribe(ILinxObserver<T> observer) => _source.Subscribe(observer);

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sWaitingAccepting = 1;
                private const int _sWaiting = 2;
                private const int _sNext = 3;
                private const int _sLast = 4;
                private const int _sFinal = 5;

                private readonly LatestOneEnumerable<T> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private readonly Observer _observer;
                private int _state;
                private T _current, _next;
                private bool _currentMutable;
                private Exception _error;

                public Enumerator(LatestOneEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    _observer = new Observer(this);
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public T Current
                {
                    get
                    {
                        var state = Atomic.Lock(ref _state);
                        var current = _current;
                        _currentMutable = false;
                        _state = state;
                        return current;
                    }
                }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsMoveNext.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sWaitingAccepting;
                            try { _enumerable._source.Subscribe(_observer); }
                            catch (Exception ex) { _observer.OnError(ex); }
                            break;

                        case _sWaiting:
                            _state = _sWaitingAccepting;
                            break;

                        case _sNext:
                            _current = _next;
                            _currentMutable = true;
                            _tsMoveNext.SetResult(true);
                            break;

                        case _sLast:
                            _current = Linx.Clear(ref _next);
                            _state = _sFinal;
                            _tsMoveNext.SetResult(true);
                            _atmbDisposed.SetResult();
                            _ctr.Dispose();
                            break;

                        case _sFinal:
                            _current = default;
                            _state = _sFinal;
                            _tsMoveNext.SetExceptionOrResult(_error, false);
                            break;

                        default: // accepting ???
                            _state = state;
                            throw new Exception(_state + "???");
                    }

                    return _tsMoveNext.Task;
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
                    if (_error != null)
                    {
                        _state = state;
                        return;
                    }

                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sWaitingAccepting:
                            _error = error;
                            _state = _sWaitingAccepting;
                            try { _cts.Cancel(); } catch { /**/ }
                            _ctr.Dispose();
                            break;

                        case _sWaiting:
                        case _sNext:
                            _error = error;
                            _state = _sWaiting;
                            try { _cts.Cancel(); } catch { /**/ }
                            _ctr.Dispose();
                            break;

                        case _sLast:
                            _error = error;
                            _next = default;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private sealed class Observer : ILinxObserver<T>
                {
                    private readonly Enumerator _e;
                    public Observer(Enumerator enumerator) => _e = enumerator;

                    public CancellationToken Token => _e._cts.Token;

                    public bool OnNext(T value)
                    {
                        Token.ThrowIfCancellationRequested();
                        var state = Atomic.Lock(ref _e._state);

                        if (_e._error != null) // race condition, token should be canceld soon
                        {
                            _e._state = state;
                            return false;
                        }

                        switch (state)
                        {
                            case _sWaitingAccepting:
                                _e._current = value;
                                _e._currentMutable = true;
                                _e._state = _sWaiting;
                                _e._tsMoveNext.SetResult(true);
                                return true;

                            case _sWaiting:
                                if (_e._currentMutable)
                                {
                                    _e._current = value;
                                    _e._state = _sWaiting;
                                }
                                else
                                {
                                    _e._next = value;
                                    _e._state = _sNext;
                                }
                                return true;

                            case _sNext:
                                _e._next = value;
                                _e._state = _sNext;
                                return true;

                            case _sLast:
                            case _sFinal:
                                _e._state = state;
                                return false;

                            default: // initial???
                                _e._state = state;
                                throw new Exception(state + "???");
                        }
                    }

                    public void OnError(Exception error)
                    {
                        _e.OnError(error ?? new ArgumentNullException(nameof(error)));
                        Complete();
                    }

                    public void OnCompleted() => Complete();

                    private void Complete()
                    {
                        var state = Atomic.Lock(ref _e._state);
                        switch (state)
                        {
                            case _sWaitingAccepting:
                                _e._next = _e._current = default;
                                _e._state = _sFinal;
                                _e._ctr.Dispose();
                                _e._atmbDisposed.SetResult();
                                if (_e._error == null)
                                {
                                    _e._tsMoveNext.SetResult(false);
                                    try { _e._cts.Cancel(); } catch { /**/ }
                                }
                                else
                                    _e._tsMoveNext.SetException(_e._error);
                                break;

                            case _sWaiting:
                                _e._next = default;
                                _e._state = _sFinal;
                                _e._ctr.Dispose();
                                if (_e._error == null)
                                    try { _e._cts.Cancel(); } catch { /**/ }
                                _e._atmbDisposed.SetResult();
                                break;

                            case _sNext:
                                _e._current = _e._next;
                                _e._state = _sLast;
                                try { _e._cts.Cancel(); } catch { /**/ }
                                break;

                            case _sLast:
                            case _sFinal:
                                _e._state = state;
                                break;

                            default: // initial???
                                _e._state = state;
                                throw new Exception(state + "???");
                        }
                    }
                }
            }
        }
    }
}
