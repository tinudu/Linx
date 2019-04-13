namespace Linx.Reactive
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, TResult>(source1, source2, resultSelector);

        private sealed class ZipEnumerable<T1, T2, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly Func<T1, T2, TResult> _resultSelector;

            public ZipEnumerable(
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
                private const int _n = 2;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, T3, TResult>(source1, source2, source3, resultSelector);

        private sealed class ZipEnumerable<T1, T2, T3, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly Func<T1, T2, T3, TResult> _resultSelector;

            public ZipEnumerable(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                Func<T1, T2, T3, TResult> resultSelector)
            {
                _source1 = source1 ?? throw new ArgumentNullException(nameof(source1));
                _source2 = source2 ?? throw new ArgumentNullException(nameof(source2));
                _source3 = source3 ?? throw new ArgumentNullException(nameof(source3));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _n = 3;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, T3, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private IAsyncEnumerator<T3> _ae3;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, T3, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            Func<T1, T2, T3, T4, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, T3, T4, TResult>(source1, source2, source3, source4, resultSelector);

        private sealed class ZipEnumerable<T1, T2, T3, T4, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly Func<T1, T2, T3, T4, TResult> _resultSelector;

            public ZipEnumerable(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                IAsyncEnumerable<T4> source4,
                Func<T1, T2, T3, T4, TResult> resultSelector)
            {
                _source1 = source1 ?? throw new ArgumentNullException(nameof(source1));
                _source2 = source2 ?? throw new ArgumentNullException(nameof(source2));
                _source3 = source3 ?? throw new ArgumentNullException(nameof(source3));
                _source4 = source4 ?? throw new ArgumentNullException(nameof(source4));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _n = 4;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, T3, T4, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private IAsyncEnumerator<T3> _ae3;
                private IAsyncEnumerator<T4> _ae4;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, T3, T4, T5, TResult>(source1, source2, source3, source4, source5, resultSelector);

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly Func<T1, T2, T3, T4, T5, TResult> _resultSelector;

            public ZipEnumerable(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                IAsyncEnumerable<T4> source4,
                IAsyncEnumerable<T5> source5,
                Func<T1, T2, T3, T4, T5, TResult> resultSelector)
            {
                _source1 = source1 ?? throw new ArgumentNullException(nameof(source1));
                _source2 = source2 ?? throw new ArgumentNullException(nameof(source2));
                _source3 = source3 ?? throw new ArgumentNullException(nameof(source3));
                _source4 = source4 ?? throw new ArgumentNullException(nameof(source4));
                _source5 = source5 ?? throw new ArgumentNullException(nameof(source5));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _n = 5;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private IAsyncEnumerator<T3> _ae3;
                private IAsyncEnumerator<T4> _ae4;
                private IAsyncEnumerator<T5> _ae5;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult>(source1, source2, source3, source4, source5, source6, resultSelector);

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly IAsyncEnumerable<T6> _source6;
            private readonly Func<T1, T2, T3, T4, T5, T6, TResult> _resultSelector;

            public ZipEnumerable(
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
                _source3 = source3 ?? throw new ArgumentNullException(nameof(source3));
                _source4 = source4 ?? throw new ArgumentNullException(nameof(source4));
                _source5 = source5 ?? throw new ArgumentNullException(nameof(source5));
                _source6 = source6 ?? throw new ArgumentNullException(nameof(source6));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _n = 6;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private IAsyncEnumerator<T3> _ae3;
                private IAsyncEnumerator<T4> _ae4;
                private IAsyncEnumerator<T5> _ae5;
                private IAsyncEnumerator<T6> _ae6;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            Produce(_enumerable._source6, 5, (e, ae) => e._ae6 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current, _ae6.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, T7, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult>(source1, source2, source3, source4, source5, source6, source7, resultSelector);

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> : IAsyncEnumerable<TResult>
        {
            private readonly IAsyncEnumerable<T1> _source1;
            private readonly IAsyncEnumerable<T2> _source2;
            private readonly IAsyncEnumerable<T3> _source3;
            private readonly IAsyncEnumerable<T4> _source4;
            private readonly IAsyncEnumerable<T5> _source5;
            private readonly IAsyncEnumerable<T6> _source6;
            private readonly IAsyncEnumerable<T7> _source7;
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, TResult> _resultSelector;

            public ZipEnumerable(
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
                _source3 = source3 ?? throw new ArgumentNullException(nameof(source3));
                _source4 = source4 ?? throw new ArgumentNullException(nameof(source4));
                _source5 = source5 ?? throw new ArgumentNullException(nameof(source5));
                _source6 = source6 ?? throw new ArgumentNullException(nameof(source6));
                _source7 = source7 ?? throw new ArgumentNullException(nameof(source7));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _n = 7;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private IAsyncEnumerator<T3> _ae3;
                private IAsyncEnumerator<T4> _ae4;
                private IAsyncEnumerator<T5> _ae5;
                private IAsyncEnumerator<T6> _ae6;
                private IAsyncEnumerator<T7> _ae7;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            Produce(_enumerable._source6, 5, (e, ae) => e._ae6 = ae);
                            Produce(_enumerable._source7, 6, (e, ae) => e._ae7 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current, _ae6.Current, _ae7.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

        /// <summary>
        /// Merges sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            IAsyncEnumerable<T8> source8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector)
            => new ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(source1, source2, source3, source4, source5, source6, source7, source8, resultSelector);

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : IAsyncEnumerable<TResult>
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

            public ZipEnumerable(
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
                _source3 = source3 ?? throw new ArgumentNullException(nameof(source3));
                _source4 = source4 ?? throw new ArgumentNullException(nameof(source4));
                _source5 = source5 ?? throw new ArgumentNullException(nameof(source5));
                _source6 = source6 ?? throw new ArgumentNullException(nameof(source6));
                _source7 = source7 ?? throw new ArgumentNullException(nameof(source7));
                _source8 = source8 ?? throw new ArgumentNullException(nameof(source8));
                _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<TResult>
            {
                private const int _n = 8;

                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private int _active;
                private IAsyncEnumerator<T1> _ae1;
                private IAsyncEnumerator<T2> _ae2;
                private IAsyncEnumerator<T3> _ae3;
                private IAsyncEnumerator<T4> _ae4;
                private IAsyncEnumerator<T5> _ae5;
                private IAsyncEnumerator<T6> _ae6;
                private IAsyncEnumerator<T7> _ae7;
                private IAsyncEnumerator<T8> _ae8;
                private readonly CoroutineCompletionSource[] _ccssPushing = new CoroutineCompletionSource[_n];
                private uint _ccssPushingMask;
                private int _state;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _active = _n;
                            _state = _sPulling;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            Produce(_enumerable._source6, 5, (e, ae) => e._ae6 = ae);
                            Produce(_enumerable._source7, 6, (e, ae) => e._ae7 = ae);
                            Produce(_enumerable._source8, 7, (e, ae) => e._ae8 = ae);
                            break;
                        case _sPushing:
                            Debug.Assert(_ccssPushingMask == (1U << _n) - 1);
                            _ccssPushingMask = 0;
                            _state = _sPulling;
                            foreach (var ccs in _ccssPushing)
                                ccs.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            _ccsMoveNext.SetCompleted(new Exception(state + "???"), false);
                            break;
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;

                        case _sPulling:
                        case _sPushing:
                            Debug.Assert(_active > 0);
                            var m = _ccssPushingMask;
                            _ccssPushingMask = 0;
                            _state = state == _sPulling ? _sCancelingPulling : _sCanceling;
                            _eh.Cancel();
                            if (m != 0)
                            {
                                var ex = new OperationCanceledException(_eh.InternalToken);
                                foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                    ccs.SetCompleted(ex);
                            }
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

                private void OnCompleted(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _eh.Cancel();
                                _atmbDisposed.SetResult();
                                _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = _sCancelingPulling;
                                _eh.Cancel();
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        case _sCanceling:
                        case _sCancelingPulling:
                            Debug.Assert(_active > 0);
                            _eh.SetInternalError(error);
                            if (--_active == 0)
                            {
                                Debug.Assert(_ccssPushingMask == 0);
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                                if (state == _sCancelingPulling)
                                    _ccsMoveNext.SetCompleted(_eh.Error, false);
                            }
                            else
                            {
                                var m = _ccssPushingMask;
                                _ccssPushingMask = 0;
                                _state = state;
                                if (m != 0)
                                {
                                    var ex = new OperationCanceledException(_eh.InternalToken);
                                    foreach (var ccs in _ccssPushing.Where((x, i) => (m & (1U << i)) != 0))
                                        ccs.SetCompleted(ex);
                                }
                            }
                            break;

                        default: // Initial, Pushing, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, IAsyncEnumerator<T>> setEnumerator)
                {
                    Exception error;
                    try
                    {
                        _eh.InternalToken.ThrowIfCancellationRequested();

                        var ccsPushing = _ccssPushing[index] = CoroutineCompletionSource.Init();
                        var flag = 1U << index;
                        var ae = source.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            setEnumerator(this, ae);

                            while (await ae.MoveNextAsync())
                            {
                                var state = Atomic.Lock(ref _state);
                                switch (state)
                                {
                                    case _sPulling:
                                        ccsPushing.Reset(false);
                                        _ccssPushingMask |= flag;
                                        if (_ccssPushingMask == (1U << _n) - 1)
                                        {
                                            try { Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current, _ae6.Current, _ae7.Current, _ae8.Current); }
                                            catch
                                            {
                                                _ccssPushingMask &= ~flag;
                                                _state = _sPulling;
                                                throw;
                                            }
                                            _state = _sPushing;
                                            _ccsMoveNext.SetCompleted(null, true);
                                        }
                                        else
                                            _state = _sPulling;
                                        await ccsPushing.Awaiter;
                                        _eh.InternalToken.ThrowIfCancellationRequested();
                                        break;

                                    case _sCanceling:
                                    case _sCancelingPulling:
                                        _state = state;
                                        throw new OperationCanceledException(_eh.InternalToken);

                                    default: // Initial, Pushing, Final???
                                        _state = state;
                                        throw new Exception(state + "???");
                                }
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    OnCompleted(error);
                }
            }
        }

    }
}
