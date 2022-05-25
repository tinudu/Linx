using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    private abstract class ZipIteratorBase<TResult> : IAsyncEnumerable<TResult>, IAsyncEnumerator<TResult>
    {
        private const int _sEnumerator = 0;
        private const int _sIdle = 1;
        private const int _sMoveNext = 2;
        private const int _sCanceled = 3;
        private const int _sDisposing = 4;
        private const int _sDisposed = 5;

        private readonly CancellationTokenSource _cts = new();
        private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new();
        private readonly AsyncTaskMethodBuilder _atmbDisposed;
        private CancellationTokenRegistration _ctr;
        private int _state;
        private TResult? _current;
        private Exception? _error;
        private int _nMoveNext;
        private int _nProducers;

        protected ZipIteratorBase(int nProducers) => _nProducers = nProducers;

        protected abstract ZipIteratorBase<TResult> Clone();

        protected abstract void PulseAll();

        protected abstract TResult GetCurrent();

        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken token)
        {
            if (Atomic.CompareExchange(ref _state, _sIdle, _sEnumerator) != _sEnumerator)
                return Clone().GetAsyncEnumerator(token);

            if (token.CanBeCanceled)
                _ctr = token.Register(() => Cancel(new OperationCanceledException(token), false));
            return this;
        }

        public TResult Current => _current!;

        public ValueTask<bool> MoveNextAsync()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sEnumerator:
                case _sMoveNext:
                    _state = state;
                    throw new InvalidOperationException();

                case _sIdle:
                    _tsMoveNext.Reset();
                    _nMoveNext = _nProducers;
                    _state = _sMoveNext;
                    PulseAll();
                    return _tsMoveNext.Task;

                case _sCanceled:
                case _sDisposing:
                    _current = default;
                    SetDisposing();
                    _tsMoveNext.Reset();
                    _tsMoveNext.SetExceptionOrResult(_error, false);
                    return _tsMoveNext.Task;

                case _sDisposed:
                    _state = _sDisposed;
                    _tsMoveNext.Reset();
                    _tsMoveNext.SetExceptionOrResult(_error, false);
                    return _tsMoveNext.Task;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        public ValueTask DisposeAsync()
        {
            Cancel(AsyncEnumeratorDisposedException.Instance, true);
            return new(_atmbDisposed.Task);
        }

        private void Cancel()
        {
            _ctr.Dispose();
            _cts.TryCancel();
            PulseAll();
        }

        private void SetDisposing()
        {
            if (_nProducers > 0)
                _state = _sDisposing;
            else
            {
                _current = default;
                _state = _sDisposed;
                _atmbDisposed.SetResult();
            }
        }

        private void Cancel(Exception? error, bool disposing)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sEnumerator:
                    _state = _sEnumerator;
                    throw new InvalidOperationException();

                case _sIdle:
                    _error = error;
                    if (disposing)
                        SetDisposing();
                    else
                        _state = _sCanceled;
                    Cancel();
                    break;

                case _sMoveNext:
                    _error = error;
                    _current = default;
                    SetDisposing();
                    Cancel();
                    _tsMoveNext.SetExceptionOrResult(error, false);
                    break;

                case _sCanceled when !disposing:
                    _state = _sCanceled;
                    break;

                case _sCanceled:
                case _sDisposing:
                    SetDisposing();
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    break;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        protected struct Producer<T>
        {
            public readonly IAsyncEnumerable<T> Source;
            private readonly ZipIteratorBase<TResult> _parent;
            private readonly ManualResetValueTaskSource<bool> _tsIdle;
            private bool _isIdle;
            private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

            public Producer(IAsyncEnumerable<T> source, ZipIteratorBase<TResult> parent)
            {
                Source = source;
                _parent = parent;
                _tsIdle = new();
                _isIdle = true;
                _enumerator = default;

                Produce();
            }

            public T GetCurrent() => _enumerator.Current;

            public void Pulse()
            {
                var parentState = Atomic.Lock(ref _parent._state);
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
                        if (Atomic.Read(in _parent._state) != _sMoveNext)
                            return;

                        bool all;
                        TResult? current;
                        if (Interlocked.Decrement(ref _parent._nMoveNext) > 0)
                        {
                            all = false;
                            current = default;
                        }
                        else
                        {
                            all = true;
                            current = _parent.GetCurrent();
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
                                break;

                            case _sCanceled:
                            case _sDisposing:
                            case _sDisposed:
                                _parent._state = parentState;
                                return;

                            default:
                                _parent._state = parentState;
                                throw new Exception(parentState + "???");
                        }

                        if (!await _tsIdle.Task.ConfigureAwait(false))
                            return;
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    _enumerator = default;
                    Interlocked.Decrement(ref _parent._nProducers);
                    _parent.Cancel(error, false);
                }
            }
        }
    }
}
