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

        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, T3, TResult>(this
            IAsyncEnumerable<T1> source1, 
            IAsyncEnumerable<T2> source2, 
            IAsyncEnumerable<T3> source3, 
            Func<T1, T2, T3, TResult> resultSelector) 
            => new CombineLatestEnumerable<T1, T2, T3, TResult>(source1, source2, source3, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, T3, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly Func<T1, T2, T3, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1, 
                IAsyncEnumerable<T2> source2, 
                IAsyncEnumerable<T3> source3, 
                Func<T1, T2, T3, TResult> resultSelector)
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

                    public int Unassigned = 3;
                    public int Active = 3;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;
                    public T3 Value3;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, T3, TResult> resultSelector) => resultSelector(Value1, Value2, Value3);
                }

                private readonly CombineLatestEnumerable<T1, T2, T3, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

                public Enumerator(CombineLatestEnumerable<T1, T2, T3, TResult> enumerable, CancellationToken token)
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
                            Subscribe(_enumerable._source3, (c, v) => c.Value3 = v);
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

        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, T3, T4, TResult>(this
            IAsyncEnumerable<T1> source1, 
            IAsyncEnumerable<T2> source2, 
            IAsyncEnumerable<T3> source3, 
            IAsyncEnumerable<T4> source4, 
            Func<T1, T2, T3, T4, TResult> resultSelector) 
            => new CombineLatestEnumerable<T1, T2, T3, T4, TResult>(source1, source2, source3, source4, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, T3, T4, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly Func<T1, T2, T3, T4, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1, 
                IAsyncEnumerable<T2> source2, 
                IAsyncEnumerable<T3> source3, 
                IAsyncEnumerable<T4> source4, 
                Func<T1, T2, T3, T4, TResult> resultSelector)
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

                    public int Unassigned = 4;
                    public int Active = 4;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;
                    public T3 Value3;
                    public T4 Value4;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, T3, T4, TResult> resultSelector) => resultSelector(Value1, Value2, Value3, Value4);
                }

                private readonly CombineLatestEnumerable<T1, T2, T3, T4, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

                public Enumerator(CombineLatestEnumerable<T1, T2, T3, T4, TResult> enumerable, CancellationToken token)
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
                            Subscribe(_enumerable._source3, (c, v) => c.Value3 = v);
                            Subscribe(_enumerable._source4, (c, v) => c.Value4 = v);
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

        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, T3, T4, T5, TResult>(this
            IAsyncEnumerable<T1> source1, 
            IAsyncEnumerable<T2> source2, 
            IAsyncEnumerable<T3> source3, 
            IAsyncEnumerable<T4> source4, 
            IAsyncEnumerable<T5> source5, 
            Func<T1, T2, T3, T4, T5, TResult> resultSelector) 
            => new CombineLatestEnumerable<T1, T2, T3, T4, T5, TResult>(source1, source2, source3, source4, source5, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, T3, T4, T5, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly Func<T1, T2, T3, T4, T5, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1, 
                IAsyncEnumerable<T2> source2, 
                IAsyncEnumerable<T3> source3, 
                IAsyncEnumerable<T4> source4, 
                IAsyncEnumerable<T5> source5, 
                Func<T1, T2, T3, T4, T5, TResult> resultSelector)
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

                    public int Unassigned = 5;
                    public int Active = 5;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;
                    public T3 Value3;
                    public T4 Value4;
                    public T5 Value5;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, T3, T4, T5, TResult> resultSelector) => resultSelector(Value1, Value2, Value3, Value4, Value5);
                }

                private readonly CombineLatestEnumerable<T1, T2, T3, T4, T5, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

                public Enumerator(CombineLatestEnumerable<T1, T2, T3, T4, T5, TResult> enumerable, CancellationToken token)
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
                            Subscribe(_enumerable._source3, (c, v) => c.Value3 = v);
                            Subscribe(_enumerable._source4, (c, v) => c.Value4 = v);
                            Subscribe(_enumerable._source5, (c, v) => c.Value5 = v);
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

        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, T3, T4, T5, T6, TResult>(this
            IAsyncEnumerable<T1> source1, 
            IAsyncEnumerable<T2> source2, 
            IAsyncEnumerable<T3> source3, 
            IAsyncEnumerable<T4> source4, 
            IAsyncEnumerable<T5> source5, 
            IAsyncEnumerable<T6> source6, 
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector) 
            => new CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, TResult>(source1, source2, source3, source4, source5, source6, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly IAsyncEnumerable<T6> _source6;
            private readonly Func<T1, T2, T3, T4, T5, T6, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1, 
                IAsyncEnumerable<T2> source2, 
                IAsyncEnumerable<T3> source3, 
                IAsyncEnumerable<T4> source4, 
                IAsyncEnumerable<T5> source5, 
                IAsyncEnumerable<T6> source6, 
                Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector)
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

                    public int Unassigned = 6;
                    public int Active = 6;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;
                    public T3 Value3;
                    public T4 Value4;
                    public T5 Value5;
                    public T6 Value6;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector) => resultSelector(Value1, Value2, Value3, Value4, Value5, Value6);
                }

                private readonly CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

                public Enumerator(CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, TResult> enumerable, CancellationToken token)
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
                            Subscribe(_enumerable._source3, (c, v) => c.Value3 = v);
                            Subscribe(_enumerable._source4, (c, v) => c.Value4 = v);
                            Subscribe(_enumerable._source5, (c, v) => c.Value5 = v);
                            Subscribe(_enumerable._source6, (c, v) => c.Value6 = v);
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

        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, T3, T4, T5, T6, T7, TResult>(this
            IAsyncEnumerable<T1> source1, 
            IAsyncEnumerable<T2> source2, 
            IAsyncEnumerable<T3> source3, 
            IAsyncEnumerable<T4> source4, 
            IAsyncEnumerable<T5> source5, 
            IAsyncEnumerable<T6> source6, 
            IAsyncEnumerable<T7> source7, 
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector) 
            => new CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult>(source1, source2, source3, source4, source5, source6, source7, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly IAsyncEnumerable<T6> _source6;
            private readonly IAsyncEnumerable<T7> _source7;
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1, 
                IAsyncEnumerable<T2> source2, 
                IAsyncEnumerable<T3> source3, 
                IAsyncEnumerable<T4> source4, 
                IAsyncEnumerable<T5> source5, 
                IAsyncEnumerable<T6> source6, 
                IAsyncEnumerable<T7> source7, 
                Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector)
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

                    public int Unassigned = 7;
                    public int Active = 7;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;
                    public T3 Value3;
                    public T4 Value4;
                    public T5 Value5;
                    public T6 Value6;
                    public T7 Value7;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector) => resultSelector(Value1, Value2, Value3, Value4, Value5, Value6, Value7);
                }

                private readonly CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

                public Enumerator(CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> enumerable, CancellationToken token)
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
                            Subscribe(_enumerable._source3, (c, v) => c.Value3 = v);
                            Subscribe(_enumerable._source4, (c, v) => c.Value4 = v);
                            Subscribe(_enumerable._source5, (c, v) => c.Value5 = v);
                            Subscribe(_enumerable._source6, (c, v) => c.Value6 = v);
                            Subscribe(_enumerable._source7, (c, v) => c.Value7 = v);
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

        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        public static IAsyncEnumerable<TResult> CombineLatest<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this
            IAsyncEnumerable<T1> source1, 
            IAsyncEnumerable<T2> source2, 
            IAsyncEnumerable<T3> source3, 
            IAsyncEnumerable<T4> source4, 
            IAsyncEnumerable<T5> source5, 
            IAsyncEnumerable<T6> source6, 
            IAsyncEnumerable<T7> source7, 
            IAsyncEnumerable<T8> source8, 
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector) 
            => new CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(source1, source2, source3, source4, source5, source6, source7, source8, resultSelector);

        private sealed class CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly IAsyncEnumerable<T6> _source6;
            private readonly IAsyncEnumerable<T7> _source7;
            private readonly IAsyncEnumerable<T8> _source8;
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _resultSelector;

            public CombineLatestEnumerable(
                IAsyncEnumerable<T1> source1, 
                IAsyncEnumerable<T2> source2, 
                IAsyncEnumerable<T3> source3, 
                IAsyncEnumerable<T4> source4, 
                IAsyncEnumerable<T5> source5, 
                IAsyncEnumerable<T6> source6, 
                IAsyncEnumerable<T7> source7, 
                IAsyncEnumerable<T8> source8, 
                Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector)
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

                    public int Unassigned = 8;
                    public int Active = 8;
                    public bool HasNext;
                    public T1 Value1;
                    public T2 Value2;
                    public T3 Value3;
                    public T4 Value4;
                    public T5 Value5;
                    public T6 Value6;
                    public T7 Value7;
                    public T8 Value8;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public TResult GetResult(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector) => resultSelector(Value1, Value2, Value3, Value4, Value5, Value6, Value7, Value8);
                }

                private readonly CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
                private Context _context = new Context();
                private int _state;
                private Exception _error;

                public TResult Current { get; private set; }

                public Enumerator(CombineLatestEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> enumerable, CancellationToken token)
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
                            Subscribe(_enumerable._source3, (c, v) => c.Value3 = v);
                            Subscribe(_enumerable._source4, (c, v) => c.Value4 = v);
                            Subscribe(_enumerable._source5, (c, v) => c.Value5 = v);
                            Subscribe(_enumerable._source6, (c, v) => c.Value6 = v);
                            Subscribe(_enumerable._source7, (c, v) => c.Value7 = v);
                            Subscribe(_enumerable._source8, (c, v) => c.Value8 = v);
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