namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskProviders;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this IAsyncEnumerable<T> source) => new LatestOneEnumerable<T>(source);

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

            public LatestOneEnumerable(IAsyncEnumerable<T> source)
            {
                Debug.Assert(source != null);
                _source = source;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0; // not enumerating
                private const int _sPulling = 1; // pending MoveNext
                private const int _sCurrentMutable = 2; // Current set, but not retrieved yet
                private const int _sCurrent = 3; // Current read only
                private const int _sNext = 4; // Next available
                private const int _sLast = 5; // done enumerating, last available
                private const int _sCanceling = 6; // cancellation requested
                private const int _sCancelingPulling = 7; // canceling and pending MoveNext
                private const int _sFinal = 8; // final stage with or without error

                private readonly LatestOneEnumerable<T> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;
                private ManualResetProvider<bool> _vtsPull = TaskProvider.ManualReset<bool>();
                private T _current, _next;

                public Enumerator(LatestOneEnumerable<T> enumerable, CancellationToken token)
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
                    _vtsPull.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            Produce();
                            break;

                        case _sCurrentMutable:
                        case _sCurrent:
                            _state = _sPulling;
                            break;

                        case _sNext:
                            _current = _next;
                            _state = _sCurrentMutable;
                            _vtsPull.SetResult(true);
                            break;

                        case _sLast:
                            _current = _next;
                            _next = default;
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            _vtsPull.SetResult(true);
                            break;

                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;

                        case _sFinal:
                            _state = _sFinal;
                            _current = default;
                            _vtsPull.SetExceptionOrResult(_eh.Error, false);
                            break;

                        default: // Pulling, CancelingPulling???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _vtsPull.Task;
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
                            _next = default;
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                            _eh.SetExternalError(error);
                            _next = default;
                            _state = _sCancelingPulling;
                            _eh.Cancel();
                            break;

                        case _sCurrent:
                        case _sCurrentMutable:
                        case _sNext:
                            _eh.SetExternalError(error);
                            _next = default;
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
                                        _vtsPull.SetResult(true);
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCurrentMutable:
                                        _current = current;
                                        _state = _sCurrentMutable;
                                        break;

                                    case _sCurrent:
                                    case _sNext:
                                        _next = current;
                                        _state = _sNext;
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
                                _current = _next = default;
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _vtsPull.SetExceptionOrResult(_eh.Error, false);
                                break;

                            case _sCurrentMutable:
                            case _sCurrent:
                                _next = default;
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                break;

                            case _sNext:
                                if (_eh.Error == null)
                                    _state = _sLast;
                                else
                                {
                                    _next = default;
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
                                _vtsPull.SetExceptionOrResult(_eh.Error, false);
                                break;

                            default: // Initial, Last, Final???
                                _state = state;
                                throw new Exception(_state + "???");
                        }
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
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;
                private ManualResetProvider<bool> _tpPull = TaskProvider.ManualReset<bool>();
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