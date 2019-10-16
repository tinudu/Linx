namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Observable;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source) => source.ToLinxObservable().Latest();

        /// <summary>
        /// Ignores all but the latest <paramref name="max"/> elements.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source, int max)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
            return max == 1 ? (IAsyncEnumerable<T>)new LatestOneEnumerable<T>(source) : new LatestManyEnumerable<T>(source, max);
        }

        private sealed class LatestOneEnumerable<T> : AsyncEnumerableBase<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            public LatestOneEnumerable(IAsyncEnumerable<T> source) => _source = source;

            public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(_source, token);

            public override string ToString() => "Latest";

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmittingMutable = 2;
                private const int _sEmitting = 3;
                private const int _sCompleted = 4;

                private sealed class Context
                {
                    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

                    public bool HasNext;
                    public T Next;

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }
                }

                private readonly IAsyncEnumerable<T> _source;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private readonly CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private Context _context = new Context();
                private int _state;
                private T _current;
                private Exception _error;

                public Enumerator(IAsyncEnumerable<T> source, CancellationToken token)
                {
                    _source = source;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
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
                            Subscribe();
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
                            if (_context == null)
                                _state = _sCompleted;
                            else
                            {
                                _error = error;
                                _context = null;
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

                private async void Subscribe()
                {
                    try
                    {
                        _context.Token.ThrowIfCancellationRequested();
                        var ae = _source.WithCancellation(_context.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                _context.Token.ThrowIfCancellationRequested();
                                var current = ae.Current;

                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sAccepting:
                                        _current = current;
                                        _state = _sEmittingMutable;
                                        _tsMoveNext.SetResult(true);
                                        break;

                                    case _sEmittingMutable:
                                        _current = current;
                                        _state = _sEmittingMutable;
                                        break;

                                    case _sEmitting:
                                        _context.Next = current;
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
                    finally
                    {
                        var state = Atomic.Lock(ref _state);
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
                }
            }
        }

        private sealed class LatestManyEnumerable<T> : AsyncEnumerableBase<T>
        {
            private readonly IAsyncEnumerable<T> _source;
            private readonly int _max;

            public LatestManyEnumerable(IAsyncEnumerable<T> source, int max)
            {
                Debug.Assert(source != null);
                Debug.Assert(max >= 2);
                _source = source;
                _max = max;
            }

            public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Latest";

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmittingMutable = 2;
                private const int _sEmitting = 3;
                private const int _sCompleted = 4;

                private sealed class Context
                {
                    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

                    public CancellationToken Token => _cts.Token;

                    public void TryCancel()
                    {
                        try { _cts.Cancel(); }
                        catch { /**/ }
                    }

                    public bool HasNext => Nexts != null && Nexts.Count > 0;
                    public Queue<T> Nexts;
                }

                private readonly LatestManyEnumerable<T> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private readonly CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private Context _context = new Context();
                private int _state;
                private T _current;
                private Exception _error;

                public Enumerator(LatestManyEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
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
                            Subscribe();
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
                                _current = _context.Nexts.Dequeue();
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
                                _current = _context.Nexts.Dequeue();
                                if (_context.HasNext)
                                    _state = _sCompleted;
                                else
                                {
                                    _context = null;
                                    _state = _sCompleted;
                                    _ctr.Dispose();
                                    _atmbDisposed.SetResult();
                                }
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
                            _context.Nexts = null;
                            _current = default;
                            _state = _sEmitting;
                            _ctr.Dispose();
                            _context.TryCancel();
                            _tsMoveNext.SetException(error);
                            break;

                        case _sEmittingMutable:
                        case _sEmitting:
                            _error = error;
                            _context.Nexts = null;
                            _state = _sEmitting;
                            _ctr.Dispose();
                            _context.TryCancel();
                            break;

                        case _sCompleted:
                            if (_context == null)
                                _state = _sCompleted;
                            else
                            {
                                _error = error;
                                _context = null;
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

                private async void Subscribe()
                {
                    try
                    {
                        _context.Token.ThrowIfCancellationRequested();
                        var ae = _enumerable._source.WithCancellation(_context.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                _context.Token.ThrowIfCancellationRequested();
                                var current = ae.Current;

                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sAccepting:
                                        _current = current;
                                        _state = _sEmittingMutable;
                                        _tsMoveNext.SetResult(true);
                                        break;

                                    case _sEmittingMutable:
                                        _current = current;
                                        _state = _sEmittingMutable;
                                        break;

                                    case _sEmitting:
                                        try
                                        {
                                            if (_context.Nexts == null)
                                                _context.Nexts = new Queue<T>();
                                            else if (_context.Nexts.Count >= _enumerable._max)
                                                _context.Nexts.Dequeue();
                                            _context.Nexts.Enqueue(current);
                                        }
                                        finally
                                        {
                                            _state = _sEmitting;
                                        }
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
                    finally
                    {
                        var state = Atomic.Lock(ref _state);
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
                }
            }
        }
    }
}