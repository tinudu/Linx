namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector)
            => new CombineLatestEnumerable<T1, T2, TResult>(source1, source2, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly Func<T1, T2, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                Func<T1, T2, TResult> resultSelector)
            {
                _source1 = source1 ?? throw new ArgumentNullException(nameof(source1));
                _source2 = source2 ?? throw new ArgumentNullException(nameof(source2));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmittingMutable = 2;
                private const int _sEmitting = 3;
                private const int _sCompleted = 4;

                private sealed class Context
                {
                    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

                    public int Unassigned = 2;
                    public int Active = 2;
                    public T1 Next1;
                    public T2 Next2;
                    public bool HasNext;
                    public TResult Next;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, TResult> resultSelector) => resultSelector(Next1, Next2);
                }

                private readonly CombineLatestEnumerable<T1, T2, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private TResult _current;
                private Exception _error;

                public TResult Current
                {
                    get
                    {
                        var state = Atomic.Lock(ref _state);
                        var current = _current;
                        _state = state == _sEmittingMutable ? _sEmitting : state;
                        return current;
                    }
                }

                public Enumerator(CombineLatestEnumerable<T1, T2, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsMoveNext.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            Subscribe(_enumerable._source1, (c, v) => c.Next1 = v);
                            Subscribe(_enumerable._source2, (c, v) => c.Next2 = v);
                            break;

                        case _sEmittingMutable:
                        case _sEmitting:
                            if (_error != null)
                            {
                                _current = default;
                                _state = _sEmitting;
                                _tsMoveNext.SetException(_error);
                            }
                            else if (_context.HasNext)
                            {
                                _current = _context.Next;
                                _context.HasNext = false;
                                _state = _sEmittingMutable;
                                _tsMoveNext.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                            if (_context != null)
                            {
                                Debug.Assert(_error == null && _context.HasNext);
                                _current = _context.Next;
                                _context = null;
                                _state = _sCompleted;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                                _tsMoveNext.SetResult(true);
                            }
                            else
                            {
                                _current = default;
                                _state = _sCompleted;
                                _tsMoveNext.SetExceptionOrResult(_error, false);
                            }
                            break;

                        default: // Accepting???
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
                            _context = null;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            _error = error;
                            _current = default;
                            _state = _sEmitting;
                            _ctr.Dispose();
                            _context.TryCancel();
                            _tsMoveNext.SetException(error);
                            break;

                        case _sEmittingMutable:
                        case _sEmitting:
                            _error = error;
                            _state = _sEmitting;
                            _ctr.Dispose();
                            _context.TryCancel();
                            break;

                        case _sCompleted:
                            var ctx = Linx.Clear(ref _context);
                            if (ctx == null)
                                _state = _sCompleted;
                            else
                            {
                                _error = error;
                                _state = _sCompleted;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    if (--_context.Active > 0)
                    {
                        _state = state;
                        return;
                    }

                    var ctx = _context;
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert(_error == null && !ctx.HasNext);
                            _context = null;
                            _current = default;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            ctx.TryCancel();
                            _atmbDisposed.SetResult();
                            _tsMoveNext.SetResult(false);
                            break;

                        case _sEmittingMutable:
                        case _sEmitting:
                            if (_error != null)
                            {
                                _context = null;
                                _state = _sCompleted;
                                _atmbDisposed.SetResult();
                            }
                            else if (ctx.HasNext)
                            {
                                _state = _sCompleted;
                                ctx.TryCancel();
                            }
                            else
                            {
                                _context = null;
                                _state = _sCompleted;
                                _ctr.Dispose();
                                ctx.TryCancel();
                                _atmbDisposed.SetResult();
                            }
                            break;

                        default: // initial, completed???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Subscribe<T>(IAsyncEnumerable<T> source, Action<Context, T> setNext)
                {
                    try
                    {
                        _context.Token.ThrowIfCancellationRequested();
                        var ae = source.WithCancellation(_context.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        try
                        {
                            var any = false;
                            while (await ae.MoveNextAsync())
                            {
                                _context.Token.ThrowIfCancellationRequested();
                                var current = ae.Current;

                                var state = Atomic.Lock(ref _state);

                                if (!Linx.Exchange(ref any, true))
                                    _context.Unassigned--;

                                setNext(_context, current);
                                if (_context.Unassigned > 0)
                                {
                                    _state = state;
                                    continue;
                                }

                                TResult next;
                                try { next = _context.GetResult(_enumerable._resultSelector); }
                                catch { _state = state; throw; }

                                switch (state)
                                {
                                    case _sAccepting:
                                        _current = next;
                                        _state = _sEmittingMutable;
                                        _tsMoveNext.SetResult(true);
                                        break;

                                    case _sEmittingMutable:
                                        _current = next;
                                        _state = _sEmittingMutable;
                                        break;

                                    case _sEmitting:
                                        _context.Next = next;
                                        _context.HasNext = true;
                                        _state = _sEmitting;
                                        break;

                                    default: // initial, completed???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync(); }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
                }
            }
        }

    }
}