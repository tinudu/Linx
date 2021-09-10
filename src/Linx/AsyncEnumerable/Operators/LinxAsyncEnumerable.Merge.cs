namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources is null) throw new ArgumentNullException(nameof(sources));

            return maxConcurrent switch
            {
                1 => sources.Concat(),
                > 1 => new MergeIterator<T>(sources, maxConcurrent),
                _ => throw new ArgumentOutOfRangeException(nameof(maxConcurrent)),
            };
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources is null) throw new ArgumentNullException(nameof(sources));

            return maxConcurrent switch
            {
                1 => sources.Concat(),
                > 1 => new MergeIterator<T>(sources.ToAsyncEnumerable(), maxConcurrent),
                _ => throw new ArgumentOutOfRangeException(nameof(maxConcurrent)),
            };
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            if (first is null) throw new ArgumentNullException(nameof(first));
            if (second is null) throw new ArgumentNullException(nameof(second));
            return new MergeIterator<T>(new[] { first, second }.ToAsyncEnumerable(), int.MaxValue);
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, params IAsyncEnumerable<T>[] sources)
        {
            if (first is null) throw new ArgumentNullException(nameof(first));
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (sources is null) throw new ArgumentNullException(nameof(sources));
            return new MergeIterator<T>(sources.Prepend(second).Prepend(first).ToAsyncEnumerable(), int.MaxValue);
        }

        private sealed class MergeIterator<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            const int _sInitial = 0; // GetAsyncEnumerator was not called
            const int _sEmitting = 1; // between MoveNextAsync calls
            const int _sAccepting = 2; // pending MoveNextAsync
            const int _sError = 3; // error occurred on the producer side, but queue still contains items
            const int _sFinal = 4; // completion is determined, state won't change from here

            private readonly IAsyncEnumerable<IAsyncEnumerable<T>> _sources;
            private readonly int _maxConcurrent;
            private readonly IEnumerable<int> _capacities;
            private readonly int _initialCapacity;

            private readonly CancellationTokenSource _cts = new();
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private ManualResetValueTaskSource<bool> _tsMaxConcurrent = new();
            private ManualResetValueTaskSource<bool> _tsEmitting;
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed;
            private int _state;
            private Exception _error;
            private int _active;
            private (ManualResetValueTaskSource<bool>, T)[] _queue;
            private int _offset, _count;

            public MergeIterator(IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent)
            {
                Debug.Assert(sources is not null);
                Debug.Assert(maxConcurrent >= 2);

                _sources = sources;
                _maxConcurrent = maxConcurrent;
                _capacities = Linx.Capacities(maxConcurrent);
            }

            private MergeIterator(MergeIterator<T> parent)
            {
                _sources = parent._sources;
                _maxConcurrent = parent._maxConcurrent;
                _capacities = parent._capacities;
                _initialCapacity = parent._initialCapacity;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial) // enumerated a second time, delegate to a new instance
                {
                    _state = state;
                    return new MergeIterator<T>(this).GetAsyncEnumerator(token);
                }

                _active = 1;
                _state = _sEmitting;
                ProduceOuter();

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));

                return this;
            }

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sInitial;
                        throw new InvalidOperationException(); // called on the IAsyncEnumerable<T> interface

                    case _sAccepting:
                        _state = _sAccepting;
                        throw new InvalidOperationException(); // not reentrant
                }

                _tsAccepting.Reset();

                if (state == _sEmitting) // make sure _tsEmitting gets released
                {
                    var tsEmitting = Linx.Clear(ref _tsEmitting);

                    if (_count > 0) // yield the next item from the queue
                    {
                        var (ts, item) = Dequeue();
                        Current = item;
                        _tsEmitting = ts;
                        _state = _sEmitting;
                        tsEmitting.SetResult(true);

                        _tsAccepting.SetResult(true);
                        return _tsAccepting.Task;
                    }

                    // go _sAccepting and check later
                    _state = _sAccepting;
                    tsEmitting.SetResult(true); // this may change state
                    state = Atomic.Lock(ref _state);
                }

                switch (state)
                {
                    case _sEmitting: // caused by releasing previous _tsEmitting
                        break;

                    case _sAccepting: // releasing previous _tsEmitting didn't change state
                        if (_tsMaxConcurrent is not null && _active <= _maxConcurrent) // unblock outer to produce more
                        {
                            var tsMaxConcurrent = Linx.Clear(ref _tsMaxConcurrent);
                            _state = _sAccepting;
                            tsMaxConcurrent.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sError:
                        Debug.Assert(_count > 0);
                        var (ts, item) = Dequeue();
                        Current = item;
                        if (_count == 0)
                        {
                            _state = _sFinal;
                            _ctr.Dispose();
                            _queue = null;
                        }
                        else
                            _state = _sError;
                        ts.SetResult(false);
                        _tsAccepting.SetResult(true);
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance); ;
                Current = default;
                return new(_atmbDisposed.Task);
            }

            private (ManualResetValueTaskSource<bool>, T) Dequeue()
            {
                Debug.Assert(_count > 0);

                var result = Linx.Clear(ref _queue[_offset++]);
                if (--_count == 0)
                {
                    _offset = 0;
                    if (_queue.Length > _initialCapacity)
                        _queue = null;
                }
                else if (_offset == _queue.Length)
                    _offset = 0;
                return result;
            }

            private void SetCompleted(Exception errorOrNot)
            {
                var state = Atomic.Lock(ref _state);

                Debug.Assert(_active > 0);
                var complete = --_active == 0;

                switch (state)
                {
                    case _sEmitting:
                        if (errorOrNot is not null)
                        {
                            if (_count == 0)
                                SetFinal(_sEmitting, errorOrNot);
                            else // go _sError
                            {
                                _error = errorOrNot;
                                var tsMaxConcurrent = Linx.Clear(ref _tsMaxConcurrent);
                                var tsEmitting = Linx.Clear(ref _tsEmitting);
                                // could/should complete ValueTasks in the queue, but this would require
                                // making a copy while holding the lock, which requires error handling.
                                // Design desicion: not worth it, dispose producers in the queue on dequeue.
                                _state = _sError;
                                tsMaxConcurrent?.SetResult(false);
                                tsEmitting.SetResult(false);
                                _cts.TryCancel();
                            }
                        }
                        else // completed without error
                        {
                            if (complete)
                                SetFinal(_sEmitting, null);
                            else
                                _state = _sEmitting;
                        }
                        throw new NotImplementedException();

                    case _sAccepting:
                        if (errorOrNot is null && !complete)
                        {
                            var tsMaxConcurrent = Linx.Clear(ref _tsMaxConcurrent);
                            _state = _sAccepting;
                            tsMaxConcurrent?.SetResult(true); // because _active decreased
                        }
                        else
                        {
                            Current = default;
                            SetFinal(_sAccepting, errorOrNot);
                        }
                        break;

                    case _sError:
                    case _sFinal:
                        _state = state;
                        break;

                    default:
                        _state = state;
                        Debug.Fail(state + "???");
                        break;
                }

                if (complete)
                    _atmbDisposed.SetResult();
            }

            private void SetFinal(Exception errorOrNot) => SetFinal(Atomic.Lock(ref _state), errorOrNot);

            private void SetFinal(int state, Exception errorOrNot)
            {
                Debug.Assert((state & Atomic.LockBit) == 0 && _state == (state | Atomic.LockBit));

                switch (state)
                {
                    case _sInitial: // DisposeAsync called on IAsyncEnumerator<T>
                        _state = _sInitial;
                        throw new InvalidOperationException();

                    case _sEmitting:
                    case _sAccepting:
                    case _sError:
                        _error = errorOrNot;
                        var tsMaxConcurrent = Linx.Clear(ref _tsMaxConcurrent);
                        var tsEmitting = Linx.Clear(ref _tsEmitting);
                        _state = _sFinal;

                        _ctr.Dispose();
                        if (state != _sError)
                            _cts.TryCancel();
                        tsMaxConcurrent?.SetResult(false);
                        tsEmitting?.SetResult(false);
                        while (_count > 0)
                            Dequeue().Item1.SetResult(false);
                        _queue = null;
                        if (state == _sAccepting)
                            _tsAccepting?.SetExceptionOrResult(errorOrNot, false);
                        break;

                    case _sFinal: // nothing to do
                        _state = _sFinal;
                        return;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            private async void ProduceOuter()
            {
                Exception error = null;
                try
                {
                    var tsMaxConcurrent = _tsMaxConcurrent;
                    if (!await tsMaxConcurrent.Task.ConfigureAwait(false)) // completes on first call to MoveNext or SetFinal
                        return;
                    tsMaxConcurrent.Reset();

                    await foreach (var inner in _sources.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);

                        // _active is 1 + number of inner producers; allow to exceed _maxConcurrent by one
                        while (state == _sEmitting || state == _sAccepting && _active > _maxConcurrent)
                        {
                            _tsMaxConcurrent = tsMaxConcurrent;
                            _state = state;
                            if (!await tsMaxConcurrent.Task.ConfigureAwait(false))
                                return;
                            tsMaxConcurrent.Reset();

                            state = Atomic.Lock(ref _state);
                        }

                        switch (state)
                        {
                            case _sEmitting:
                            case _sAccepting:
                                _active++;
                                _state = state;
                                ProduceInner(inner);
                                break;

                            default: // _sError or _sFinal
                                _state = state;
                                return;
                        }
                    }
                }
                catch (Exception ex) { error = ex; }
                finally { SetCompleted(error); }
            }

            private async void ProduceInner(IAsyncEnumerable<T> source)
            {
                Exception error = null;
                try
                {
                    ManualResetValueTaskSource<bool> tsEmitting = new();
                    await foreach (var item in source.WithCancellation(_cts.Token).ConfigureAwait(false))
                    {
                        var state = Atomic.Lock(ref _state);
                        switch (_state)
                        {
                            case _sEmitting:
                                try // enque
                                {
                                    if (_queue == null)
                                    {
                                        Debug.Assert(_count == 0 && _offset == 0);
                                        _queue = new (ManualResetValueTaskSource<bool>, T)[_initialCapacity];
                                        _queue[0] = (tsEmitting, item);
                                        _count = 1;
                                    }
                                    else if (_count == _queue.Length) // increase _queue size
                                    {
                                        Debug.Assert(_count < _maxConcurrent);
                                        var q = new (ManualResetValueTaskSource<bool>, T)[_capacities.TakeWhile(c => c > _count).Last()];
                                        if (_offset == 0)
                                            Array.Copy(_queue, q, _count);
                                        else
                                        {
                                            var c = _queue.Length - _offset;
                                            Array.Copy(_queue, _offset, q, 0, c);
                                            Array.Copy(_queue, 0, q, c, _offset);
                                            _offset = 0;
                                        }
                                        q[_count++] = (tsEmitting, item);
                                        _queue = q;
                                    }
                                    else if (_offset == 0)
                                        _queue[_count++] = (tsEmitting, item);
                                    else
                                    {
                                        var ix = _offset - _queue.Length + _count++;
                                        _queue[ix >= 0 ? ix : ix + _queue.Length] = (tsEmitting, item);
                                    }
                                }
                                finally { _state = _sEmitting; }
                                break;

                            case _sAccepting:
                                Debug.Assert(_count == 0);
                                Current = item;
                                _tsEmitting = tsEmitting;
                                _state = _sEmitting;
                                _tsAccepting.SetResult(true);
                                break;

                            default: // _sError, _sFinal
                                _state = state;
                                return;
                        }

                        if (!await tsEmitting.Task.ConfigureAwait(false))
                            return;
                        tsEmitting.Reset();
                    }
                }
                catch (Exception ex) { error = ex; }
                finally { SetCompleted(error); }
            }
        }
    }
}
