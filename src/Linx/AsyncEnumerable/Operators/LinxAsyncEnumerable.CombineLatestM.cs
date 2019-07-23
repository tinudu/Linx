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
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, TResult>(this IAsyncEnumerable<T1> source1, IAsyncEnumerable<T2> source2, Func<T1, T2, TResult> resultSelector) => new CombineLatestEnumerable<T1, T2, TResult>(source1, source2, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly Func<T1, T2, TResult> _resultSelector;

            public CombineLatestEnumerable(IAsyncEnumerable<T1> source1, IAsyncEnumerable<T2> source2, Func<T1, T2, TResult> resultSelector)
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
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;

                private sealed class Context
                {
                    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

                    public int Unassigned = 2;
                    public int Active = 2;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, TResult> resultSelector) => resultSelector(Value1, Value2);
                }

                private readonly CombineLatestEnumerable<T1, T2, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

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
                            Subscribe(_enumerable._source1, (c, v) => c.Value1 = v);
                            Subscribe(_enumerable._source2, (c, v) => c.Value2 = v);
                            break;

                        case _sEmitting:
                            if (_error != null)
                            {
                                Current = default;
                                _state = _sEmitting;
                                _tsMoveNext.SetException(_error);
                            }
                            else if (_context.HasNext)
                            {
                                try
                                {
                                    Current = _context.GetResult(_enumerable._resultSelector);
                                    _context.HasNext = false;
                                    _state = _sEmitting;
                                }
                                catch (Exception ex)
                                {
                                    _error = ex;
                                    Current = default;
                                    _state = _sEmitting;
                                    _ctr.Dispose();
                                    _context.TryCancel();
                                }
                                _tsMoveNext.SetExceptionOrResult(_error, true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                            if (_context != null)
                            {
                                Debug.Assert(_error == null && _context.HasNext);
                                try
                                {
                                    Current = _context.GetResult(_enumerable._resultSelector);
                                }
                                catch (Exception ex)
                                {
                                    Current = default;
                                    _error = ex;
                                }

                                _context = null;
                                _state = _sCompleted;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                                _tsMoveNext.SetExceptionOrResult(_error, true);
                            }
                            else
                            {
                                Current = default;
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
                            Current = default;
                            _state = _sEmitting;
                            _ctr.Dispose();
                            _context.TryCancel();
                            _tsMoveNext.SetException(error);
                            break;

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
                            Current = default;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            ctx.TryCancel();
                            _atmbDisposed.SetResult();
                            _tsMoveNext.SetResult(false);
                            break;

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

                private async void Subscribe<T>(IAsyncEnumerable<T> source, Action<Context, T> setValue)
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

                                setValue(_context, current);
                                if (_context.Unassigned > 0)
                                {
                                    _state = state;
                                    continue;
                                }

                                switch (state)
                                {
                                    case _sAccepting:
                                        try { Current = _context.GetResult(_enumerable._resultSelector); }
                                        catch
                                        {
                                            _state = _sAccepting;
                                            throw;
                                        }

                                        _state = _sEmitting;
                                        _tsMoveNext.SetResult(true);
                                        break;

                                    case _sEmitting:
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