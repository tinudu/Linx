namespace Linx.Reactive
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
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
                private const int _sCurrentMutable = 1;
                private const int _sNext = 2;
                private const int _sPulling = 3;
                private const int _sLast = 4;
                private const int _sCanceling = 5;
                private const int _sCancelingPulling = 6;
                private const int _sFinal = 7;

                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private readonly Func<T1, T2, TResult> _resultSelector;
                private CancellationTokenRegistration _ctr;
                private CoAwaiterCompleter<bool> _ccsPull = CoAwaiterCompleter<bool>.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state;
                private TResult _current;
                private Tuple _next;
                private Exception _error;

                public Enumerator(CombineLatestEnumerable<T1, T2, TResult> enumerable, CancellationToken token)
                {
                    _resultSelector = enumerable._resultSelector;
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                    var valuesCountDown = 2;
                    var completedCountDown = 2;
                    Produce(enumerable._source1, (e, v) => e._next.Value1 = v);
                    Produce(enumerable._source2, (e, v) => e._next.Value2 = v);

                    void OnCompleted(Exception error, bool any)
                    {
                        Debug.Assert(any || valuesCountDown > 0);

                        if (_cts.IsCancellationRequested && error is OperationCanceledException oce && oce.CancellationToken == _cts.Token) error = null;

                        var state = Atomic.Lock(ref _state);

                        if (--completedCountDown > 0) // cancel?
                        {
                            switch (state)
                            {
                                case _sInitial:
                                case _sPulling:
                                    _state = state;
                                    if (error != null || !any) Cancel(error);
                                    break;

                                case _sCanceling:
                                case _sCancelingPulling:
                                    _state = state;
                                    if (error != null) _error = error;
                                    break;

                                default:
                                    _state = state;
                                    throw new Exception(state + "???");
                            }

                            return;
                        }

                        switch (state)
                        {
                            case _sInitial:
                            case _sCurrentMutable:
                                _next = default;
                                _error = error;
                                _state = _sFinal;
                                try { _cts.Cancel(); } catch {/**/}
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                                break;

                            case _sNext:
                                if (error != null)
                                {
                                    _next = default;
                                    _error = error;
                                    _state = _sFinal;
                                    _ctr.Dispose();
                                    _atmbDisposed.SetResult();
                                }
                                else
                                    _state = _sLast;
                                try { _cts.Cancel(); } catch {/**/}
                                break;

                            case _sPulling:
                                _next = default;
                                _error = error;
                                _state = _sFinal;
                                try { _cts.Cancel(); } catch {/**/}
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                                _ccsPull.SetCompleted(_error, false);
                                break;

                            case _sCanceling:
                                if (error != null) _error = error;
                                _state = _sFinal;
                                break;

                            case _sCancelingPulling:
                                if (error != null) _error = error;
                                _current = default;
                                _state = _sFinal;
                                _ccsPull.SetCompleted(_error, false);
                                break;

                            default: // Last, Final???
                                _state = state;
                                throw new Exception(_state + "???");
                        }
                    }

                    async void Produce<T>(IAsyncEnumerable<T> source, Action<Enumerator, T> setNext)
                    {
                        Exception error;
                        var any = false;
                        try
                        {
                            _cts.Token.ThrowIfCancellationRequested();

                            var ae = source.GetAsyncEnumerator(_cts.Token);
                            try
                            {
                                while (await ae.MoveNextAsync())
                                {
                                    var state = Atomic.Lock(ref _state);

                                    if (!any)
                                    {
                                        valuesCountDown--;
                                        any = true;
                                    }

                                    switch (state)
                                    {
                                        case _sInitial:
                                            try { setNext(this, ae.Current); }
                                            catch { _state = _sInitial; throw; }
                                            _state = valuesCountDown == 0 ? _sNext : _sInitial;
                                            break;

                                        case _sNext:
                                            try { setNext(this, ae.Current); }
                                            finally { _state = _sNext; }
                                            break;

                                        case _sCurrentMutable:
                                            try
                                            {
                                                setNext(this, ae.Current);
                                                _current = GetResult();
                                            }
                                            finally { _state = _sCurrentMutable; }
                                            break;

                                        case _sPulling:
                                            try
                                            {
                                                setNext(this, ae.Current);
                                                _current = GetResult();
                                            }
                                            catch { _state = _sPulling; throw; }
                                            if (valuesCountDown == 0)
                                            {
                                                _state = _sCurrentMutable;
                                                _ccsPull.SetCompleted(null, true);
                                            }
                                            else
                                                _state = _sPulling;
                                            break;

                                        case _sCanceling:
                                        case _sCancelingPulling:
                                            _state = state;
                                            Atomic.WaitCanceled(_cts.Token);
                                            throw new OperationCanceledException(_cts.Token);

                                        default: // Last, Final???
                                            _state = state;
                                            throw new Exception(state + "???");
                                    }
                                }
                            }
                            finally { await ae.DisposeAsync().ConfigureAwait(false); }

                            error = null;
                        }
                        catch (Exception ex) { error = ex; }

                        OnCompleted(error, any);
                    }
                }

                public TResult Current
                {
                    get
                    {
                        Atomic.TestAndSet(ref _state, _sCurrentMutable, _sInitial);
                        return _current;
                    }
                }

                private TResult GetResult() => _resultSelector(_next.Value1, _next.Value2);

                public ICoAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext = false)
                {
                    _ccsPull.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sCurrentMutable:
                            _state = _sPulling;
                            break;

                        case _sNext:
                            try
                            {
                                _current = GetResult();
                                _state = _sCurrentMutable;
                                _ccsPull.SetCompleted(null, true);
                            }
                            catch (Exception error)
                            {
                                _error = error;
                                _next = default;
                                _state = _sCancelingPulling;
                                _ctr.Dispose();
                                try { _cts.Cancel(); } catch { /**/ }
                            }
                            break;

                        case _sLast:
                            try
                            {
                                _current = GetResult();
                                _next = default;
                                _state = _sFinal;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                                _ccsPull.SetCompleted(null, true);
                            }
                            catch (Exception error)
                            {
                                _current = default;
                                _next = default;
                                _error = error;
                                _state = _sFinal;
                                _ctr.Dispose();
                                _ccsPull.SetCompleted(_error, false);
                            }
                            break;

                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;

                        case _sFinal:
                            _current = default;
                            _state = _sFinal;
                            _ccsPull.SetCompleted(_error, false);
                            break;

                        default: // Pulling, CancelingPulling???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _ccsPull.Awaiter;
                }

                public Task DisposeAsync()
                {
                    if (_state <= _sLast) Cancel(new ObjectDisposedException(nameof(IAsyncEnumerator<TResult>)));
                    return _atmbDisposed.Task;
                }

                private void Cancel(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                        case _sCurrentMutable:
                        case _sNext:
                            _error = error;
                            _next = default;
                            _state = _sCanceling;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            break;

                        case _sPulling:
                            _error = error;
                            _next = default;
                            _state = _sCancelingPulling;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            break;

                        case _sLast:
                            _error = error;
                            _next = default;
                            _state = _sCanceling;
                            _ctr.Dispose();
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

                private struct Tuple
                {
                    public T1 Value1;
                    public T2 Value2;
                }
            }
        }
    }
}
