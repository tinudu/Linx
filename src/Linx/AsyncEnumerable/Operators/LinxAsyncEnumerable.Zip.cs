using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasks;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector)
            => new ZipIterator<T1, T2, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector)
            => new ZipIterator<T1, T2, T3, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                source3 ?? throw new ArgumentNullException(nameof(source3)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            Func<T1, T2, T3, T4, TResult> resultSelector)
            => new ZipIterator<T1, T2, T3, T4, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                source3 ?? throw new ArgumentNullException(nameof(source3)),
                source4 ?? throw new ArgumentNullException(nameof(source4)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector)
            => new ZipIterator<T1, T2, T3, T4, T5, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                source3 ?? throw new ArgumentNullException(nameof(source3)),
                source4 ?? throw new ArgumentNullException(nameof(source4)),
                source5 ?? throw new ArgumentNullException(nameof(source5)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector)
            => new ZipIterator<T1, T2, T3, T4, T5, T6, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                source3 ?? throw new ArgumentNullException(nameof(source3)),
                source4 ?? throw new ArgumentNullException(nameof(source4)),
                source5 ?? throw new ArgumentNullException(nameof(source5)),
                source6 ?? throw new ArgumentNullException(nameof(source6)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
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
            => new ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                source3 ?? throw new ArgumentNullException(nameof(source3)),
                source4 ?? throw new ArgumentNullException(nameof(source4)),
                source5 ?? throw new ArgumentNullException(nameof(source5)),
                source6 ?? throw new ArgumentNullException(nameof(source6)),
                source7 ?? throw new ArgumentNullException(nameof(source7)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
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
            => new ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
                source1 ?? throw new ArgumentNullException(nameof(source1)),
                source2 ?? throw new ArgumentNullException(nameof(source2)),
                source3 ?? throw new ArgumentNullException(nameof(source3)),
                source4 ?? throw new ArgumentNullException(nameof(source4)),
                source5 ?? throw new ArgumentNullException(nameof(source5)),
                source6 ?? throw new ArgumentNullException(nameof(source6)),
                source7 ?? throw new ArgumentNullException(nameof(source7)),
                source8 ?? throw new ArgumentNullException(nameof(source8)),
                resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

        private sealed class ZipIterator<T1, T2, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 2;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Func<T1, T2, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                Func<T1, T2, TResult> resultSelector)
            {
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

        private sealed class ZipIterator<T1, T2, T3, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 3;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Producer<T3> _p3;
            private readonly Func<T1, T2, T3, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                Func<T1, T2, T3, TResult> resultSelector)
            {
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _p3 = new Producer<T3>(source3, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, T3, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _p3.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
                _p3.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, T3, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, T3, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent(), _parent._p3.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

        private sealed class ZipIterator<T1, T2, T3, T4, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 4;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Producer<T3> _p3;
            private readonly Producer<T4> _p4;
            private readonly Func<T1, T2, T3, T4, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                IAsyncEnumerable<T4> source4,
                Func<T1, T2, T3, T4, TResult> resultSelector)
            {
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _p3 = new Producer<T3>(source3, this);
                _p4 = new Producer<T4>(source4, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, T3, T4, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _p3.Source,
                        _p4.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
                _p3.Unblock();
                _p4.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, T3, T4, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, T3, T4, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent(), _parent._p3.GetCurrent(), _parent._p4.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

        private sealed class ZipIterator<T1, T2, T3, T4, T5, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 5;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Producer<T3> _p3;
            private readonly Producer<T4> _p4;
            private readonly Producer<T5> _p5;
            private readonly Func<T1, T2, T3, T4, T5, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                IAsyncEnumerable<T4> source4,
                IAsyncEnumerable<T5> source5,
                Func<T1, T2, T3, T4, T5, TResult> resultSelector)
            {
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _p3 = new Producer<T3>(source3, this);
                _p4 = new Producer<T4>(source4, this);
                _p5 = new Producer<T5>(source5, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, T3, T4, T5, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _p3.Source,
                        _p4.Source,
                        _p5.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
                _p3.Unblock();
                _p4.Unblock();
                _p5.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, T3, T4, T5, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, T3, T4, T5, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent(), _parent._p3.GetCurrent(), _parent._p4.GetCurrent(), _parent._p5.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

        private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 6;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Producer<T3> _p3;
            private readonly Producer<T4> _p4;
            private readonly Producer<T5> _p5;
            private readonly Producer<T6> _p6;
            private readonly Func<T1, T2, T3, T4, T5, T6, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                IAsyncEnumerable<T4> source4,
                IAsyncEnumerable<T5> source5,
                IAsyncEnumerable<T6> source6,
                Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector)
            {
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _p3 = new Producer<T3>(source3, this);
                _p4 = new Producer<T4>(source4, this);
                _p5 = new Producer<T5>(source5, this);
                _p6 = new Producer<T6>(source6, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, T3, T4, T5, T6, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _p3.Source,
                        _p4.Source,
                        _p5.Source,
                        _p6.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
                _p3.Unblock();
                _p4.Unblock();
                _p5.Unblock();
                _p6.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, T3, T4, T5, T6, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, T3, T4, T5, T6, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent(), _parent._p3.GetCurrent(), _parent._p4.GetCurrent(), _parent._p5.GetCurrent(), _parent._p6.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

        private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 7;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Producer<T3> _p3;
            private readonly Producer<T4> _p4;
            private readonly Producer<T5> _p5;
            private readonly Producer<T6> _p6;
            private readonly Producer<T7> _p7;
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
                IAsyncEnumerable<T1> source1,
                IAsyncEnumerable<T2> source2,
                IAsyncEnumerable<T3> source3,
                IAsyncEnumerable<T4> source4,
                IAsyncEnumerable<T5> source5,
                IAsyncEnumerable<T6> source6,
                IAsyncEnumerable<T7> source7,
                Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector)
            {
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _p3 = new Producer<T3>(source3, this);
                _p4 = new Producer<T4>(source4, this);
                _p5 = new Producer<T5>(source5, this);
                _p6 = new Producer<T6>(source6, this);
                _p7 = new Producer<T7>(source7, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _p3.Source,
                        _p4.Source,
                        _p5.Source,
                        _p6.Source,
                        _p7.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
                _p3.Unblock();
                _p4.Unblock();
                _p5.Unblock();
                _p6.Unblock();
                _p7.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent(), _parent._p3.GetCurrent(), _parent._p4.GetCurrent(), _parent._p5.GetCurrent(), _parent._p6.GetCurrent(), _parent._p7.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

        private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
        {
            private const int _n = 8;
            private const int _sInitial = 0;
            private const int _sIdle = 1;
            private const int _sMoveNext = 2;
            private const int _sFinal = 3;

            private readonly Producer<T1> _p1;
            private readonly Producer<T2> _p2;
            private readonly Producer<T3> _p3;
            private readonly Producer<T4> _p4;
            private readonly Producer<T5> _p5;
            private readonly Producer<T6> _p6;
            private readonly Producer<T7> _p7;
            private readonly Producer<T8> _p8;
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _resultSelector;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
            private readonly AsyncTaskMethodBuilder _atmbDisposed;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private TResult? _current;
            private Exception? _error;
            private int _nMoveNext;
            private int _nDispose = _n;

            public ZipIterator(
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
                _p1 = new Producer<T1>(source1, this);
                _p2 = new Producer<T2>(source2, this);
                _p3 = new Producer<T3>(source3, this);
                _p4 = new Producer<T4>(source4, this);
                _p5 = new Producer<T5>(source5, this);
                _p6 = new Producer<T6>(source6, this);
                _p7 = new Producer<T7>(source7, this);
                _p8 = new Producer<T8>(source8, this);
                _resultSelector = resultSelector;
            }

            public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
            {
                if (Atomic.CompareExchange(ref _state, _sIdle, _sInitial) != _sInitial)
                    return new ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
                        _p1.Source,
                        _p2.Source,
                        _p3.Source,
                        _p4.Source,
                        _p5.Source,
                        _p6.Source,
                        _p7.Source,
                        _p8.Source,
                        _resultSelector).GetAsyncEnumerator(token);

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }

            public TResult Current => _current!;

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _tsMoveNext.Reset();
                        _nMoveNext = _n;
                        _state = _sMoveNext;
                        Unblock();
                        return _tsMoveNext.Task;

                    case _sFinal:
                        _tsMoveNext.Reset();
                        _state = _sFinal;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        return _tsMoveNext.Task;

                    case _sInitial:
                    case _sMoveNext:
                        _state = state;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            public async ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
                _error = AsyncEnumeratorDisposedException.Instance;
            }

            private void Unblock()
            {
                _p1.Unblock();
                _p2.Unblock();
                _p3.Unblock();
                _p4.Unblock();
                _p5.Unblock();
                _p6.Unblock();
                _p7.Unblock();
                _p8.Unblock();
            }

            private void SetFinal(Exception? error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        break;

                    case _sMoveNext:
                        _error = error;
                        _current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _cts.TryCancel();
                        Unblock();
                        _tsMoveNext.SetExceptionOrResult(error, false);
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        break;

                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private sealed class Producer<T>
            {
                public readonly IAsyncEnumerable<T> Source;
                private readonly ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _parent;
                private readonly ManualResetValueTaskSource<bool> _tsIdle = new();
                private bool _isIdle = true;
                private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

                public Producer(IAsyncEnumerable<T> source, ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult> parent)
                {
                    Source = source;
                    _parent = parent;
                    Produce();
                }

                public T GetCurrent() => _enumerator.Current;

                public void Unblock()
                {
                    var parentState = Atomic.Lock(ref _parent._state);
                    Debug.Assert(parentState is _sMoveNext or _sFinal);
                    if (_isIdle)
                    {
                        _isIdle = false;
                        _parent._state = parentState;
                        _tsIdle.SetResult(parentState == _sMoveNext);
                    }
                    else
                        _parent._state = parentState;
                }

                private async void Produce()
                {
                    Exception? error = null;
                    try
                    {
                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;

                        await using var e = _enumerator = Source.WithCancellation(_parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                        while (await e.MoveNextAsync())
                        {
                            if (Atomic.Read(ref _parent._state) != _sMoveNext)
                                return;

                            Debug.Assert(_parent._nMoveNext > 0);
                            bool all;
                            TResult? current;
                            if (Interlocked.Decrement(ref _parent._nMoveNext) == 0)
                            {
                                all = true;
                                current = _parent._resultSelector(_parent._p1.GetCurrent(), _parent._p2.GetCurrent(), _parent._p3.GetCurrent(), _parent._p4.GetCurrent(), _parent._p5.GetCurrent(), _parent._p6.GetCurrent(), _parent._p7.GetCurrent(), _parent._p8.GetCurrent());
                            }
                            else
                            {
                                all = false;
                                current = default;
                            }

                            var parentState = Atomic.Lock(ref _parent._state);
                            switch (parentState)
                            {
                                case _sMoveNext:
                                    _tsIdle.Reset();
                                    _isIdle = true;
                                    if (all)
                                    {
                                        _parent._current = current;
                                        _parent._state = _sIdle;
                                        _parent._tsMoveNext.SetResult(true);
                                    }
                                    else
                                        _parent._state = _sMoveNext;

                                    if (!await _tsIdle.Task.ConfigureAwait(false))
                                        return;
                                    break;

                                case _sFinal:
                                    _parent._state = _sFinal;
                                    return;

                                default:
                                    _parent._state = parentState;
                                    throw new Exception(parentState + "???");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        _parent.SetFinal(error);
                        _enumerator = default;
                        Debug.Assert(_parent._nDispose > 0);
                        if (Interlocked.Decrement(ref _parent._nDispose) == 0)
                            _parent._atmbDisposed.SetResult();
                    }
                }
            }
        }

    }
}
