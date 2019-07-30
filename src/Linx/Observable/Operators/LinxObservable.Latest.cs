namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
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

        private sealed class LatestOneEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly ILinxObservable<T> _source;

            public LatestOneEnumerable(ILinxObservable<T> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
                => new Enumerator(this, token);

            public override string ToString() => _source + ".Latest";

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private enum SeqState
                {
                    Waiting,
                    Next,
                    Last
                }

                private sealed class Context
                {
                    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

                    public CancellationTokenRegistration Ctr;
                    public SeqState SeqState;
                    public T Next;
                    public CancellationToken Token => _cts.Token;

                    public void Dispose()
                    {
                        Ctr.Dispose();
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }
                }

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmittingMutable = 2;
                private const int _sEmitting = 3;

                private readonly LatestOneEnumerable<T> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private Context _context = new Context();
                private int _state;
                private T _current;
                private Exception _error;

                public Enumerator(LatestOneEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _context.Ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public T Current
                {
                    get
                    {
                        var state = Atomic.Lock(ref _state);
                        var current = _current;
                        _state = state == _sEmittingMutable ? _sEmitting : state;
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
                            _state = _sAccepting;
                            try { _enumerable._source.Subscribe(new Observer(this)); }
                            catch (Exception ex) { OnError(new Exception("Error subscribing.", ex)); }
                            break;

                        case _sEmittingMutable:
                        case _sEmitting:
                            if (_context == null)
                            {
                                _current = default;
                                _state = _sEmitting;
                                _tsMoveNext.SetExceptionOrResult(_error, false);
                            }
                            else
                                switch (_context.SeqState)
                                {
                                    case SeqState.Waiting:
                                        _state = _sAccepting;
                                        break;

                                    case SeqState.Next:
                                        _current = _context.Next;
                                        _context.SeqState = SeqState.Waiting;
                                        _state = _sEmittingMutable;
                                        _tsMoveNext.SetResult(true);
                                        break;

                                    case SeqState.Last:
                                        var ctx = Linx.Clear(ref _context);
                                        _current = ctx.Next;
                                        _state = _sEmitting;
                                        ctx.Dispose();
                                        _tsMoveNext.SetResult(true);
                                        _atmbDisposed.SetResult();
                                        break;

                                    default:
                                        _state = _sAccepting;
                                        throw new Exception(_context.SeqState + "???");
                                }
                            break;

                        default: // Accepting???
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
                    var state = Atomic.Lock(ref _state);
                    if (_context == null || _error != null)
                    {
                        _state = state;
                        return;
                    }

                    _error = error;
                    var ctx = _context;
                    switch (ctx.SeqState)
                    {
                        case SeqState.Waiting:
                        case SeqState.Next:
                            ctx.SeqState = SeqState.Waiting;
                            _state = state;
                            break;

                        case SeqState.Last:
                            _context = null;
                            _state = _sEmitting;
                            _atmbDisposed.SetResult();
                            if (state == _sAccepting)
                            {
                                _current = default;
                                _tsMoveNext.SetException(error);
                            }
                            break;
                    }
                    ctx.Dispose();
                }

                private sealed class Observer : ILinxObserver<T>
                {
                    private readonly Enumerator _enumerator;

                    public Observer(Enumerator enumerator)
                    {
                        _enumerator = enumerator;
                        Token = enumerator._context.Token;
                    }

                    public CancellationToken Token { get; }

                    public bool OnNext(T value)
                    {
                        var state = Atomic.Lock(ref _enumerator._state);
                        if (_enumerator._context == null || _enumerator._context.SeqState == SeqState.Last)
                        {
                            _enumerator._state = state;
                            return false;
                        }

                        if (_enumerator._error != null)
                        {
                            _enumerator._state = state;
                            Token.ThrowIfCancellationRequested();
                            return false; // race condition? token should be canceled
                        }

                        switch (state)
                        {
                            case _sAccepting:
                                _enumerator._current = value;
                                _enumerator._context.SeqState = SeqState.Waiting;
                                _enumerator._state = _sEmittingMutable;
                                _enumerator._tsMoveNext.SetResult(true);
                                break;

                            case _sEmittingMutable:
                                _enumerator._current = value;
                                _enumerator._context.SeqState = SeqState.Waiting;
                                _enumerator._state = _sEmittingMutable;
                                break;

                            case _sEmitting:
                                _enumerator._context.Next = value;
                                _enumerator._context.SeqState = SeqState.Next;
                                _enumerator._state = _sEmitting;
                                break;

                            default:
                                _enumerator._state = state;
                                throw new Exception(state + "???");
                        }

                        return true;
                    }

                    public void OnError(Exception error)
                    {
                        _enumerator.OnError(error ?? new ArgumentNullException(nameof(error)));
                        Complete();
                    }

                    public void OnCompleted() => Complete();

                    private void Complete()
                    {
                        var state = Atomic.Lock(ref _enumerator._state);
                        var ctx = _enumerator._context;
                        if (ctx == null)
                            _enumerator._state = state;
                        else
                            switch (ctx.SeqState)
                            {
                                case SeqState.Waiting:
                                    _enumerator._context = null;
                                    _enumerator._state = _sEmitting;
                                    ctx.Dispose();
                                    _enumerator._atmbDisposed.SetResult();
                                    if (state == _sAccepting)
                                    {
                                        _enumerator._current = default;
                                        _enumerator._tsMoveNext.SetExceptionOrResult(_enumerator._error, false);
                                    }
                                    break;

                                case SeqState.Next:
                                    _enumerator._context.SeqState = SeqState.Last;
                                    _enumerator._state = _sEmitting;
                                    break;

                                case SeqState.Last:
                                    _enumerator._state = state;
                                    break;

                                default:
                                    _enumerator._state = state;
                                    throw new Exception(_enumerator._context.SeqState + "???");
                            }
                    }
                }
            }
        }
    }
}
