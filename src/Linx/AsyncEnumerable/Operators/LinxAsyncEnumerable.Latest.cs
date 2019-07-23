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
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LatestOneEnumerable<T>(source);
        }

        /// <summary>
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new ObserveLatestOneEnumerable<T>(source);
        }

        /// <summary>
        /// Ignores all but the latest <paramref name="max"/> elements.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source, int max)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return max <= 1 ? (IAsyncEnumerable<T>)new LatestOneEnumerable<T>(source) : new LatestManyEnumerable<T>(source, max);
        }

        private sealed class LatestOneEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            public LatestOneEnumerable(IAsyncEnumerable<T> source) => _source = source;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(_source, token);

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
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private CancellationTokenRegistration _ctr;
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

        private sealed class ObserveLatestOneEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IObservable<T> _source;

            public ObserveLatestOneEnumerable(IObservable<T> source)
            {
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(_source, token);

            private sealed class Enumerator : IAsyncEnumerator<T>, IObserver<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sDisposed = 4;

                private readonly IObservable<T> _source;
                private readonly CancellationToken _token;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private CancellationTokenRegistration _ctr;
                private int _state;
                private bool _hasNext;
                private T _next;
                private Exception _error;
                private IDisposable _subscription;

                public Enumerator(IObservable<T> source, CancellationToken token)
                {
                    _source = source;
                    _token = token;
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
                            _state = _sAccepting;
                            try
                            {
                                var subscription = _source.Subscribe(this);
                                state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sAccepting:
                                    case _sEmitting:
                                        _subscription = subscription;
                                        _state = state;
                                        break;

                                    default:
                                        _state = state;
                                        subscription.Dispose();
                                        break;
                                }
                            }
                            catch (Exception ex) { OnError(ex); }
                            break;

                        case _sEmitting:
                            if (_hasNext)
                            {
                                Current = _next;
                                _hasNext = false;
                                _state = _sEmitting;
                                _tsMoveNext.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                            if (_hasNext && _token.IsCancellationRequested)
                            {
                                _hasNext = false;
                                _next = default;
                                _error = new OperationCanceledException(_token);
                            }
                            if (_hasNext)
                            {
                                Current = _next;
                                _next = default;
                                _hasNext = false;
                                _state = _sCompleted;
                                _tsMoveNext.SetResult(true);
                            }
                            else
                            {
                                _state = _sCompleted;
                                _tsMoveNext.SetExceptionOrResult(_error, false);
                            }
                            break;

                        case _sDisposed:
                            _state = state;
                            _tsMoveNext.SetException(AsyncEnumeratorDisposedException.Instance);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tsMoveNext.Task;
                }

                public ValueTask DisposeAsync()
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sAccepting:
                        case _sEmitting:
                        case _sCompleted:
                            Current = _next = default;
                            _error = null;
                            _state = _sDisposed;
                            _ctr.Dispose();
                            _subscription?.Dispose();

                            if (state == _sAccepting)
                                _tsMoveNext.SetException(AsyncEnumeratorDisposedException.Instance);
                            break;

                        case _sDisposed:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return new ValueTask(Task.CompletedTask);
                }

                public void OnNext(T value)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Current = value;
                            _state = _sEmitting;
                            _tsMoveNext.SetResult(true);
                            break;

                        case _sEmitting:
                            _next = value;
                            _hasNext = true;
                            _state = _sEmitting;
                            break;

                        default:
                            _state = state;
                            break;
                    }
                }

                public void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                        case _sEmitting:
                            if (!_hasNext)
                                _next = default;

                            _state = _sCompleted;
                            _ctr.Dispose();
                            _subscription?.Dispose();

                            if (state == _sAccepting)
                            {
                                Debug.Assert(!_hasNext);
                                _tsMoveNext.SetResult(false);
                            }
                            break;

                        case _sCompleted:
                        case _sDisposed:
                            _state = _sDisposed;
                            break;

                        default: // Initial???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                public void OnError(Exception error)
                {
                    if (error == null) throw new ArgumentNullException(nameof(error));

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sAccepting:
                        case _sEmitting:
                            _hasNext = false;
                            _next = default;
                            _error = error;

                            _state = _sCompleted;
                            _ctr.Dispose();
                            _subscription?.Dispose();

                            if (state == _sAccepting)
                            {
                                Debug.Assert(!_hasNext);
                                _tsMoveNext.SetException(error);
                            }
                            break;

                        default: // completed, disposed
                            _state = state;
                            break;
                    }
                }
            }
        }

        private sealed class LatestManyEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> _source;
            private readonly int _maxSize;

            public LatestManyEnumerable(IAsyncEnumerable<T> source, int maxSize)
            {
                Debug.Assert(source != null);
                Debug.Assert(maxSize >= 2);
                _source = source;
                _maxSize = maxSize;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0; // not enumerating
                private const int _sPulling = 1; // pending MoveNext
                private const int _sCurrentMutable = 2; // Current set, but not retrieved yet
                private const int _sCurrent = 3; // Current read only, next in queue if not empty
                private const int _sLast = 4; // done enumerating, remaining in (non-empty) queue
                private const int _sCanceling = 5; // enumerating and canceled
                private const int _sCancelingPulling = 6;
                private const int _sFinal = 7;

                private readonly LatestManyEnumerable<T> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tpPull = new ManualResetValueTaskSource<bool>();
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;
                private T _current;
                private Queue<T> _next;

                public Enumerator(LatestManyEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current
                {
                    get
                    {
                        Atomic.TestAndSet(ref _state, _sCurrentMutable, _sCurrent);
                        return _current;
                    }
                }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tpPull.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            Produce();
                            break;

                        case _sCurrentMutable:
                        case _sCurrent:
                            if (_next == null || _next.Count == 0)
                                _state = _sPulling;
                            else
                            {
                                _current = _next.Dequeue(); // no exception assumed
                                _state = _sCurrentMutable;
                                _tpPull.SetResult(true);
                            }
                            break;

                        case _sLast:
                            Debug.Assert(_next.Count > 0);
                            _current = _next.Dequeue(); // no exception assumed
                            if (_next.Count > 0)
                                _state = _sLast;
                            else
                            {
                                _next = null;
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                            }
                            _tpPull.SetResult(true);
                            break;

                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            _current = default;
                            _tpPull.SetExceptionOrResult(_eh.Error, false);
                            break;

                        default: // Pulling, CancelingPulling???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tpPull.Task;
                }

                public ValueTask DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return new ValueTask(_atmbDisposed.Task);
                }

                private void Cancel(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sLast:
                            _eh.SetExternalError(error);
                            _next = null;
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                            _eh.SetExternalError(error);
                            _next = null;
                            _state = _sCancelingPulling;
                            _eh.Cancel();
                            break;

                        case _sCurrent:
                        case _sCurrentMutable:
                            _eh.SetExternalError(error);
                            _next = null;
                            _state = _sCanceling;
                            _eh.Cancel();
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce()
                {
                    Exception error;
                    try
                    {
                        var ae = _enumerable._source.WithCancellation(_eh.InternalToken).ConfigureAwait(false).GetAsyncEnumerator();
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                var current = ae.Current;

                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        _current = current;
                                        _state = _sCurrentMutable;
                                        _tpPull.SetResult(true);
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCurrentMutable:
                                        try
                                        {
                                            if (_next == null)
                                                _next = new Queue<T>();
                                            else if (_next.Count + 1 == _enumerable._maxSize)
                                                _current = _next.Dequeue();
                                            _next.Enqueue(current);
                                        }
                                        finally { _state = _sCurrentMutable; }
                                        break;

                                    case _sCurrent:
                                        try
                                        {
                                            if (_next == null)
                                                _next = new Queue<T>();
                                            else if (_next.Count == _enumerable._maxSize)
                                                _next.Dequeue();
                                            _next.Enqueue(current);
                                        }
                                        finally { _state = _sCurrent; }
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Completed, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync(); }

                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    {
                        var state = Atomic.Lock(ref _state);
                        _eh.SetInternalError(error);
                        switch (state)
                        {
                            case _sPulling:
                                _current = default;
                                _next = null;
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _tpPull.SetExceptionOrResult(_eh.Error, false);
                                break;

                            case _sCurrentMutable:
                            case _sCurrent:
                                if (_eh.Error == null && _next != null && _next.Count > 0)
                                    _state = _sLast;
                                else
                                {
                                    _next = null;
                                    _state = _sFinal;
                                    _eh.Cancel();
                                    _atmbDisposed.SetResult();
                                }
                                break;

                            case _sCanceling:
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                break;

                            case _sCancelingPulling:
                                _current = default;
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                _tpPull.SetExceptionOrResult(_eh.Error, false);
                                break;

                            default: // Initial, Last, Final???
                                _state = state;
                                throw new Exception(_state + "???");
                        }
                    }
                }
            }
        }
    }
}