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
        /// Buffers items in case the consumer is slower than the generator.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new BufferAsyncEnumerable<T>(source);
        }

        private sealed class BufferAsyncEnumerable<T> : AsyncEnumerableBase<T>
        {
            private readonly ILinxObservable<T> _source;

            public BufferAsyncEnumerable(ILinxObservable<T> source) => _source = source;

            public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(_source, token);

            public override string ToString() => "Buffer";

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sNextAccepting = 1;
                private const int _sNext = 2;
                private const int _sLast = 3;
                private const int _sFinal = 4;

                private readonly ILinxObservable<T> _source;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private readonly Observer _observer;
                private int _state;
                private Exception _error;
                private Queue<T> _queue;

                public Enumerator(ILinxObservable<T> source, CancellationToken token)
                {
                    _source = source;
                    _observer = new Observer(this, token);
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsMoveNext.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sNextAccepting;
                            try { _source.Subscribe(_observer); }
                            catch (Exception ex) { _observer.OnError(ex); }
                            break;

                        case _sNext:
                            if (_queue == null || _queue.Count == 0)
                                _state = _sNextAccepting;
                            else
                            {
                                Current = _queue.Dequeue();
                                if (_queue.Count == 0)
                                    try { _queue.TrimExcess(); }
                                    catch { _queue = null; }
                                _state = _sNext;
                                _tsMoveNext.SetResult(true);
                            }
                            break;

                        case _sLast:
                            Debug.Assert(_queue.Count > 0 && _error == null);
                            Current = _queue.Dequeue();
                            if (_queue.Count > 0)
                                _state = _sLast;
                            else
                            {
                                _queue = null;
                                _state = _sFinal;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            _tsMoveNext.SetResult(true);
                            break;

                        case _sFinal:
                            Current = default;
                            _state = _sFinal;
                            _tsMoveNext.SetExceptionOrResult(_error, false);
                            break;

                        default: // accepting???
                            _state = state;
                            throw new Exception(state + "???");
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

                        case _sNextAccepting:
                        case _sNext:
                            _error = error;
                            _queue = null;
                            _state = state;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            break;

                        case _sLast:
                            _error = error;
                            _queue = null;
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

                    public Observer(Enumerator enumerator, CancellationToken token)
                    {
                        _e = enumerator;
                        Token = token;
                    }

                    public CancellationToken Token { get; }

                    public bool OnNext(T value)
                    {
                        Token.ThrowIfCancellationRequested();

                        var state = Atomic.Lock(ref _e._state);

                        if (_e._error != null)
                        {
                            _e._state = state;
                            return false;
                        }

                        switch (state)
                        {
                            case _sNextAccepting:
                                Debug.Assert(_e._queue == null || _e._queue.Count == 0);
                                _e.Current = value;
                                _e._state = _sNext;
                                _e._tsMoveNext.SetResult(true);
                                return true;

                            case _sNext:
                                try
                                {
                                    if (_e._queue == null) _e._queue = new Queue<T>();
                                    _e._queue.Enqueue(value);
                                }
                                catch (Exception ex)
                                {
                                    _e._state = _sNext;
                                    _e.OnError(ex);
                                    return false;
                                }
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
                        _e.OnError(error ?? throw new ArgumentNullException(nameof(error)));
                        Complete();
                    }

                    public void OnCompleted() => Complete();

                    private void Complete()
                    {
                        var state = Atomic.Lock(ref _e._state);
                        switch (state)
                        {
                            case _sNextAccepting:
                                Debug.Assert(_e._queue == null || _e._queue.Count == 0);
                                Debug.Assert(_e._error == null);
                                _e.Current = default;
                                _e._queue = null;
                                _e._state = _sFinal;
                                _e._ctr.Dispose();
                                try { _e._cts.Cancel(); } catch { /**/ }
                                _e._atmbDisposed.SetResult();
                                _e._tsMoveNext.SetResult(false);
                                break;

                            case _sNext:
                                if (_e._queue != null && _e._queue.Count > 0)
                                    _e._state = _sLast;
                                else
                                {
                                    _e._queue = null;
                                    _e._state = _sFinal;
                                    _e._ctr.Dispose();
                                    if (_e._error == null)
                                        try { _e._cts.Cancel(); } catch { /**/ }
                                    _e._atmbDisposed.SetResult();
                                }
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
