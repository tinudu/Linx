using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Async;

namespace Linx.Observable;

partial class LinxObservable
{
    /// <summary>
    /// Get the most recent item in a sequence.
    /// </summary>
    public static IAsyncEnumerable<Deferred<T>> Latest<T>(this ILinxObservable<T> source)
        => new LatestIterator<T>(source);

    private sealed class LatestIterator<T> : IAsyncEnumerable<Deferred<T>>, IAsyncEnumerator<Deferred<T>>, Deferred<T>.IProvider
    {
        private const int _sEnumerator = 0;
        private const int _sIdle1st = 1;
        private const int _sIdleCurrent = 2;
        private const int _sIdleNoNext = 3;
        private const int _sIdleNext = 4;
        private const int _sMoving = 5;
        private const int _sCanceled = 6;
        private const int _sCompletedNext = 7;
        private const int _sCompletedNoNext = 8;
        private const int _sDisposing = 9;
        private const int _sDisposingMoving = 10;
        private const int _sDisposed = 11;

        private readonly ILinxObservable<T> _source;

        private readonly CancellationTokenSource _cts = new();
        private readonly ManualResetValueTaskCompleter<bool> _vtcMoving = new();
        private CancellationTokenRegistration _ctr;
        private AsyncTaskMethodBuilder _atmbDisposed = AsyncTaskMethodBuilder.Create();
        private int _state;
        private T? _current, _next;
        private short _version;
        private Exception? _error;

        public LatestIterator(ILinxObservable<T> source)
            => _source = source ?? throw new ArgumentNullException(nameof(source));

        private LatestIterator(LatestIterator<T> parent)
            => _source = parent._source;

        public IAsyncEnumerator<Deferred<T>> GetAsyncEnumerator(CancellationToken token)
        {
            if (Atomic.CompareExchange(ref _state, _sIdle1st, _sEnumerator) != _sEnumerator)
                return new LatestIterator<T>(this).GetAsyncEnumerator(token);

            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token), false));

            return this;
        }

        public Deferred<T> Current { get; private set; }

        T Deferred<T>.IProvider.GetResult(short token)
        {
            var state = Atomic.Lock(ref _state);
            if (token != _version)
            {
                _state = state;
                throw new InvalidOperationException();
            }
            var current = _current!;
            _state = state == _sIdleCurrent ? _sIdleNoNext : state;
            return current;
        }

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

                case _sIdle1st:
                    _vtcMoving.Reset();
                    _state = _sMoving;
                    Produce();
                    return _vtcMoving.ValueTask;

                case _sIdleCurrent:
                case _sIdleNoNext:
                    _vtcMoving.Reset();
                    _state = _sMoving;
                    return _vtcMoving.ValueTask;

                case _sIdleNext:
                    _vtcMoving.Reset();
                    _current = Linx.Clear(ref _next);
                    Current = new(this, unchecked(++_version));
                    _state = _sIdleCurrent;
                    _vtcMoving.SetResult(true);
                    return _vtcMoving.ValueTask;

                case _sCompletedNext:
                    _vtcMoving.Reset();
                    _current = Linx.Clear(ref _next);
                    Current = new(this, unchecked(++_version));
                    _state = _sCompletedNoNext;
                    _vtcMoving.SetResult(true);
                    _ctr.Dispose();
                    return _vtcMoving.ValueTask;

                case _sCanceled:
                case _sDisposing:
                    _vtcMoving.Reset();
                    _state = _sDisposingMoving;
                    return _vtcMoving.ValueTask;

                case _sCompletedNoNext:
                    _vtcMoving.Reset();
                    Current = new();
                    _current = default;
                    _state = _sDisposed;
                    _atmbDisposed.SetResult();
                    _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    return _vtcMoving.ValueTask;

                case _sDisposed:
                    _state = _sDisposed;
                    return _vtcMoving.ValueTask;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        public ValueTask DisposeAsync()
        {
            OnError(AsyncEnumeratorDisposedException.Instance, true);
            return new(_atmbDisposed.Task);
        }

        private void OnError(Exception error, bool disposing)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sEnumerator: // DisposeAsync on IAsyncEnumerable<>
                    _state = _sEnumerator;
                    throw new InvalidOperationException();

                case _sIdle1st: // Cancel or DisposeAsync before 1st MoveNextAsync
                    _state = _sDisposed;
                    _ctr.Dispose();
                    _atmbDisposed.SetResult();
                    _vtcMoving.SetException(error);
                    break;

                case _sIdleCurrent:
                case _sIdleNoNext:
                case _sIdleNext:
                    _error = error;
                    _next = default;
                    _state = disposing ? _sDisposing : _sCanceled;
                    _ctr.Dispose();
                    _cts.Cancel();
                    break;

                case _sMoving:
                    _error = error;
                    _next = default;
                    _state = _sDisposingMoving;
                    _ctr.Dispose();
                    _cts.Cancel();
                    break;

                case _sCanceled:
                    _state = disposing ? _sDisposing : _sCanceled;
                    break;

                case _sDisposing:
                case _sDisposingMoving:
                    _state = state;
                    break;

                case _sCompletedNext:
                    if (disposing)
                    {
                        Current = default;
                        _current = _next = default;
                        _error = null;
                        _vtcMoving.Reset();
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                    }
                    else
                    {
                        _next = default;
                        _error = error;
                        _state = _sCompletedNoNext;
                        _ctr.Dispose();
                    }
                    break;

                case _sCompletedNoNext:
                    if (disposing)
                    {
                        Current = default;
                        _current = default;
                        _vtcMoving.Reset();
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    }
                    else
                        _state = _sCompletedNoNext;
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    break;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        private async void Produce()
        {
            Exception? error = null;
            try
            {
                await LinxAsync.Yield();
                _cts.Token.ThrowIfCancellationRequested();
                await _source.Subscribe(Yield, _cts.Token);
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdleNext:
                        _error = error;
                        _state = _sCompletedNext;
                        _cts.Cancel();
                        break;

                    case _sIdleCurrent:
                    case _sIdleNoNext:
                        _error = error;
                        _state = _sCompletedNoNext;
                        _ctr.Dispose();
                        _cts.Cancel();
                        break;

                    case _sMoving:
                        Current = default;
                        _current = default;
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                        _cts.Cancel();
                        break;

                    case _sCanceled:
                        _state = _sCompletedNoNext;
                        break;

                    case _sDisposing:
                        Current = default;
                        _current = default;
                        _vtcMoving.Reset();
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                        break;

                    case _sDisposingMoving:
                        Current = default;
                        _current = default;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                        break;

                    case _sEnumerator:
                    case _sIdle1st:
                    case _sCompletedNext:
                    case _sCompletedNoNext:
                    case _sDisposed:
                    default:
                        _state = state;
                        Debug.Fail(state + "???");
                        break;
                }
            }
        }

        private bool Yield(T item)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sIdleCurrent:
                    _current = item;
                    _state = _sIdleCurrent;
                    return true;

                case _sIdleNoNext:
                case _sIdleNext:
                    _next = item;
                    _state = _sIdleNext;
                    return true;

                case _sMoving:
                    _current = item;
                    Current = new(this, unchecked(_version++));
                    _state = _sIdleCurrent;
                    _vtcMoving.SetResult(true);
                    return true;

                case _sCanceled:
                case _sCompletedNext:
                case _sCompletedNoNext:
                case _sDisposing:
                case _sDisposingMoving:
                case _sDisposed:
                    _state = state;
                    return false;

                case _sEnumerator:
                case _sIdle1st:
                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }
    }
}
