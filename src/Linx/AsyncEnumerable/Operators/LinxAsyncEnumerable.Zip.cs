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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;

                public Enumerator(ZipEnumerable<T1, T2, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, T3, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;
                private ConfiguredCancelableAsyncEnumerable<T3>.Enumerator _ae3;

                public Enumerator(ZipEnumerable<T1, T2, T3, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, T3, T4, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;
                private ConfiguredCancelableAsyncEnumerable<T3>.Enumerator _ae3;
                private ConfiguredCancelableAsyncEnumerable<T4>.Enumerator _ae4;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;
                private ConfiguredCancelableAsyncEnumerable<T3>.Enumerator _ae3;
                private ConfiguredCancelableAsyncEnumerable<T4>.Enumerator _ae4;
                private ConfiguredCancelableAsyncEnumerable<T5>.Enumerator _ae5;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;
                private ConfiguredCancelableAsyncEnumerable<T3>.Enumerator _ae3;
                private ConfiguredCancelableAsyncEnumerable<T4>.Enumerator _ae4;
                private ConfiguredCancelableAsyncEnumerable<T5>.Enumerator _ae5;
                private ConfiguredCancelableAsyncEnumerable<T6>.Enumerator _ae6;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            Produce(_enumerable._source6, 5, (e, ae) => e._ae6 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current, _ae6.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;
                private ConfiguredCancelableAsyncEnumerable<T3>.Enumerator _ae3;
                private ConfiguredCancelableAsyncEnumerable<T4>.Enumerator _ae4;
                private ConfiguredCancelableAsyncEnumerable<T5>.Enumerator _ae5;
                private ConfiguredCancelableAsyncEnumerable<T6>.Enumerator _ae6;
                private ConfiguredCancelableAsyncEnumerable<T7>.Enumerator _ae7;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            Produce(_enumerable._source6, 5, (e, ae) => e._ae6 = ae);
                            Produce(_enumerable._source7, 6, (e, ae) => e._ae7 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current, _ae6.Current, _ae7.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
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
                private const uint _allFlags = (1U << _n) - 1;

                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCompleted = 3;
                private const int _sFinal = 4;

                private readonly ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _enumerable;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool>[] _tssEmitting = new ManualResetValueTaskSource<bool>[_n];
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private CancellationTokenRegistration _ctr;
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state, _active;
                private uint _emittingFlags;
                private Exception _error;
                private ConfiguredCancelableAsyncEnumerable<T1>.Enumerator _ae1;
                private ConfiguredCancelableAsyncEnumerable<T2>.Enumerator _ae2;
                private ConfiguredCancelableAsyncEnumerable<T3>.Enumerator _ae3;
                private ConfiguredCancelableAsyncEnumerable<T4>.Enumerator _ae4;
                private ConfiguredCancelableAsyncEnumerable<T5>.Enumerator _ae5;
                private ConfiguredCancelableAsyncEnumerable<T6>.Enumerator _ae6;
                private ConfiguredCancelableAsyncEnumerable<T7>.Enumerator _ae7;
                private ConfiguredCancelableAsyncEnumerable<T8>.Enumerator _ae8;

                public Enumerator(ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                }

                public TResult Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            _active = _n;
                            Produce(_enumerable._source1, 0, (e, ae) => e._ae1 = ae);
                            Produce(_enumerable._source2, 1, (e, ae) => e._ae2 = ae);
                            Produce(_enumerable._source3, 2, (e, ae) => e._ae3 = ae);
                            Produce(_enumerable._source4, 3, (e, ae) => e._ae4 = ae);
                            Produce(_enumerable._source5, 4, (e, ae) => e._ae5 = ae);
                            Produce(_enumerable._source6, 5, (e, ae) => e._ae6 = ae);
                            Produce(_enumerable._source7, 6, (e, ae) => e._ae7 = ae);
                            Produce(_enumerable._source8, 7, (e, ae) => e._ae8 = ae);
                            break;

                        case _sEmitting:
                            _emittingFlags = 0;
                            _state = _sAccepting;
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(Atomic.CompareExchange(ref _state, _sAccepting, _sAccepting) == _sAccepting);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        default: // Accepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    OnError(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);

                }

                private void OnNext(int index)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            _emittingFlags |= 1u << index;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_ae1.Current, _ae2.Current, _ae3.Current, _ae4.Current, _ae5.Current, _ae6.Current, _ae7.Current, _ae8.Current);
                                }
                                catch (Exception ex)
                                {
                                    _state = _sAccepting;
                                    OnError(ex);
                                    return;
                                }

                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                            }
                            else
                                _state = _sAccepting;
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            _tssEmitting[index].SetResult(false);
                            break;

                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnError(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetException(error);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sEmitting:
                            _error = error;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            foreach (var ts in _tssEmitting)
                                ts.SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void OnCompleted()
                {
                    var state = Atomic.Lock(ref _state);
                    Debug.Assert(_active > 0);
                    _active--;
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            Debug.Assert(_active > 0);
                            _state = _sCompleted;
                            _ctr.Dispose();
                            _tsAccepting.SetResult(false);
                            for (var i = 0; _emittingFlags != 0; i++, _emittingFlags >>= 1)
                                if ((_emittingFlags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            break;

                        case _sCompleted:
                            if (_active == 0)
                            {
                                _state = _sFinal;
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sCompleted;
                            break;

                        default: // Initial, Emitting, Final???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, ConfiguredCancelableAsyncEnumerable<T>.Enumerator> setEnumerator)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        await using var ae = source.WithCancellation(_cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                        setEnumerator(this, ae);
                        while (await ae.MoveNextAsync())
                        {
                            ts.Reset();
                            OnNext(index);
                            if (!await ts.Task.ConfigureAwait(false))
                                break;
                        }
                    }
                    catch (Exception ex) { OnError(ex); }
                    finally { OnCompleted(); }
                }
            }
        }

    }
}
