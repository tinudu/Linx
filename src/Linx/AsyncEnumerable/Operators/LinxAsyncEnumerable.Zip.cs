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

        private sealed class ZipEnumerable<T1, T2, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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

        private sealed class ZipEnumerable<T1, T2, T3, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;
                private T3 _value3;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
                            Produce(_enumerable._source3, 2, (e, v) => e._value3 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2, _value3);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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

        private sealed class ZipEnumerable<T1, T2, T3, T4, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;
                private T3 _value3;
                private T4 _value4;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
                            Produce(_enumerable._source3, 2, (e, v) => e._value3 = v);
                            Produce(_enumerable._source4, 3, (e, v) => e._value4 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2, _value3, _value4);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;
                private T3 _value3;
                private T4 _value4;
                private T5 _value5;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
                            Produce(_enumerable._source3, 2, (e, v) => e._value3 = v);
                            Produce(_enumerable._source4, 3, (e, v) => e._value4 = v);
                            Produce(_enumerable._source5, 4, (e, v) => e._value5 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2, _value3, _value4, _value5);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, T6, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;
                private T3 _value3;
                private T4 _value4;
                private T5 _value5;
                private T6 _value6;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
                            Produce(_enumerable._source3, 2, (e, v) => e._value3 = v);
                            Produce(_enumerable._source4, 3, (e, v) => e._value4 = v);
                            Produce(_enumerable._source5, 4, (e, v) => e._value5 = v);
                            Produce(_enumerable._source6, 5, (e, v) => e._value6 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2, _value3, _value4, _value5, _value6);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;
                private T3 _value3;
                private T4 _value4;
                private T5 _value5;
                private T6 _value6;
                private T7 _value7;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
                            Produce(_enumerable._source3, 2, (e, v) => e._value3 = v);
                            Produce(_enumerable._source4, 3, (e, v) => e._value4 = v);
                            Produce(_enumerable._source5, 4, (e, v) => e._value5 = v);
                            Produce(_enumerable._source6, 5, (e, v) => e._value6 = v);
                            Produce(_enumerable._source7, 6, (e, v) => e._value7 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2, _value3, _value4, _value5, _value6, _value7);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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

        private sealed class ZipEnumerable<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : AsyncEnumerableBase<TResult>
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

            public override IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            public override string ToString() => "Zip";

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
                private T1 _value1;
                private T2 _value2;
                private T3 _value3;
                private T4 _value4;
                private T5 _value5;
                private T6 _value6;
                private T7 _value7;
                private T8 _value8;

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
                            Produce(_enumerable._source1, 0, (e, v) => e._value1 = v);
                            Produce(_enumerable._source2, 1, (e, v) => e._value2 = v);
                            Produce(_enumerable._source3, 2, (e, v) => e._value3 = v);
                            Produce(_enumerable._source4, 3, (e, v) => e._value4 = v);
                            Produce(_enumerable._source5, 4, (e, v) => e._value5 = v);
                            Produce(_enumerable._source6, 5, (e, v) => e._value6 = v);
                            Produce(_enumerable._source7, 6, (e, v) => e._value7 = v);
                            Produce(_enumerable._source8, 7, (e, v) => e._value8 = v);
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

                private void OnNext(uint flag, ManualResetValueTaskSource<bool> ts)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert((_emittingFlags & flag) == 0);
                            _emittingFlags |= flag;
                            if (_emittingFlags == _allFlags)
                            {
                                try
                                {
                                    Current = _enumerable._resultSelector(_value1, _value2, _value3, _value4, _value5, _value6, _value7, _value8);
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
                            ts.SetResult(false);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetException(error);
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
                            var flags = _emittingFlags;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            for (var i = 0; flags != 0; i++, flags >>= 1)
                                if ((flags & 1U) != 0)
                                    _tssEmitting[i].SetResult(false);
                            _cts.TryCancel();
                            _tsAccepting.SetResult(false);
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

                private async void Produce<T>(IAsyncEnumerable<T> source, int index, Action<Enumerator, T> setValue)
                {
                    try
                    {
                        _cts.Token.ThrowIfCancellationRequested();
                        var ts = _tssEmitting[index] = new ManualResetValueTaskSource<bool>();
                        var flag = 1u << index;
                        await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                        {
                            setValue(this, item);
                            ts.Reset();
                            OnNext(flag, ts);
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
