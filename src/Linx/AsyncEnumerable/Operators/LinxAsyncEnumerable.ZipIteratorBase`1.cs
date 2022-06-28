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
        private const int _sMoving = 2;
        private const int _sCanceled = 3;
        private const int _sDisposing = 4;
        private const int _sDisposingMoving = 5;
        private const int _sDisposed = 6;

        private readonly CancellationTokenSource _cts = new();
        private readonly ManualResetValueTaskSource<bool> _tsMoving = new();
        private readonly AsyncTaskMethodBuilder _atmbDisposed = AsyncTaskMethodBuilder.Create();
        private CancellationTokenRegistration _ctr;
        private int _state;
        private TResult? _current;
        private Exception? _error;
        private int _nMoving;
        private int _nProducers;

        protected ZipIteratorBase(int nProducers) => _nProducers = nProducers;

        protected abstract ZipIteratorBase<TResult> Clone();

        protected abstract void PulseAll();

        protected void Pulse(ref ManualResetValueTaskSource<bool>? tsIdle)
        {
            var state = Atomic.Lock(ref _state);
            var ts = Linx.Clear(ref tsIdle);
            _state = state;
            ts?.SetResult(state == _sMoving);
        }

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
                case _sMoving:
                case _sDisposingMoving:
                    _state = state;
                    throw new InvalidOperationException();

                case _sIdle:
                    _tsMoving.Reset();
                    _nMoving = _nProducers;
                    _state = _sMoving;
                    PulseAll();
                    return _tsMoving.Task;

                case _sCanceled:
                case _sDisposing:
                    _tsMoving.Reset();
                    SetDisposingMoving();
                    return _tsMoving.Task;

                case _sDisposed:
                    _state = _sDisposed;
                    return _tsMoving.Task;

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
                _tsMoving.Reset();
                _tsMoving.SetExceptionOrResult(_error, false);
                _state = _sDisposed;
                _atmbDisposed.SetResult();
            }
        }

        private void SetDisposingMoving()
        {
            if (_nProducers > 0)
                _state = _sDisposingMoving;
            else
            {
                _current = default;
                _state = _sDisposed;
                _atmbDisposed.SetResult();
                _tsMoving.SetExceptionOrResult(_error, false);
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

                case _sMoving:
                    _error = error;
                    SetDisposingMoving();
                    Cancel();
                    break;

                case _sCanceled when !disposing:
                    _state = _sCanceled;
                    break;

                case _sCanceled:
                case _sDisposing:
                    SetDisposing();
                    break;

                case _sDisposingMoving:
                    SetDisposingMoving();
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
            public static void Init(out Producer<T> p, IAsyncEnumerable<T> source, ZipIteratorBase<TResult> parent)
            {
                p = new(source);
                p.Produce(parent);
            }

            public readonly IAsyncEnumerable<T> Source;
            public ManualResetValueTaskSource<bool>? TsIdle;
            private ConfiguredCancelableAsyncEnumerable<T>.Enumerator _enumerator;

            public Producer(IAsyncEnumerable<T> source)
            {
                Source = source;
                TsIdle = default;
                _enumerator = default;
            }

            public T GetCurrent() => _enumerator.Current;

            private async void Produce(ZipIteratorBase<TResult> parent)
            {
                Exception? error = null;
                try
                {
                    var ts = TsIdle = new();
                    if (!await ts.Task.ConfigureAwait(false))
                        return;

                    await using var e = _enumerator = Source.WithCancellation(parent._cts.Token).ConfigureAwait(false).GetAsyncEnumerator();

                    while (await e.MoveNextAsync())
                    {
                        if (Atomic.Read(in parent._state) != _sMoving)
                            return;

                        bool all;
                        TResult? current;
                        if (Interlocked.Decrement(ref parent._nMoving) > 0)
                        {
                            all = false;
                            current = default;
                        }
                        else
                        {
                            all = true;
                            current = parent.GetCurrent();
                        }

                        var parentState = Atomic.Lock(ref parent._state);
                        switch (parentState)
                        {
                            case _sMoving:
                                ts.Reset();
                                TsIdle = ts;
                                if (all)
                                {
                                    parent._current = current;
                                    parent._state = _sIdle;
                                    parent._tsMoving.SetResult(true);
                                }
                                else
                                    parent._state = _sMoving;
                                break;

                            case _sCanceled:
                            case _sDisposing:
                            case _sDisposed:
                                parent._state = parentState;
                                return;

                            default:
                                parent._state = parentState;
                                throw new Exception(parentState + "???");
                        }

                        if (!await ts.Task.ConfigureAwait(false))
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
                    Interlocked.Decrement(ref parent._nProducers);
                    parent.Cancel(error, false);
                }
            }
        }
    }
}
