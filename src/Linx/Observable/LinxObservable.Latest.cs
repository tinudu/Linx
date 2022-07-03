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
        private const int _sEnumerator = 0; // not enumerated yet
        private const int _sInitial = 1; // before 1st call to MoveNextAsync
        private const int _sIdleCurrent = 2; // new items replace current
        private const int _sIdleNoNext = 3; // no next, next item goes to next
        private const int _sIdleNext = 4; // has next, new items replace it
        private const int _sMoving = 5; // pending MoveNextAsync
        private const int _sCanceled = 6; // canceled but not completed
        private const int _sCompletedNext = 7; // completed, but one item yet to be emitted
        private const int _sCompletedNoNext = 8; // completed
        private const int _sDisposing = 9; // not completed, pending DisposeAsync
        private const int _sDisposingMoving = 10; // not completed, pending MoveNextAsync
        private const int _sDisposed = 11; // done

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
            if (Atomic.CompareExchange(ref _state, _sInitial, _sEnumerator) != _sEnumerator)
                return new LatestIterator<T>(this).GetAsyncEnumerator(token);

            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token), false));

            return this;
        }

        public Deferred<T> Current { get; private set; }

        T Deferred<T>.IProvider.GetResult(short token)
        {
            var state = Atomic.Lock(ref _state);
            if (token != _version) // must be a previous version
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

                case _sInitial:
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

                case _sCompletedNext: // emit last item
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
                    _current = default;
                    unchecked { ++_version; }
                    Current = new();
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

                case _sInitial: // Cancel or DisposeAsync before 1st MoveNextAsync
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
                        _current = _next = default;
                        unchecked { ++_version; }
                        Current = default;
                        _error = null; // current error wins over previous
                        _vtcMoving.Reset();
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetException(error);
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
                        _current = default;
                        unchecked { ++_version; }
                        Current = default;
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
                // make 1st MoveNextAsync return incompleted
                // stay on the same context for Subscribe
                await LinxAsync.Yield();

                _cts.Token.ThrowIfCancellationRequested();
                await _source.Subscribe(Yield, _cts.Token).ConfigureAwait(false);
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
                        _current = default;
                        unchecked { ++_version; }
                        Current = default;
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
                        _current = default;
                        unchecked { ++_version; }
                        Current = default;
                        _vtcMoving.Reset();
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                        break;

                    case _sDisposingMoving:
                        _current = default;
                        unchecked { ++_version; }
                        Current = default;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                        break;

                    case _sEnumerator:
                    case _sInitial:
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

                //case _sEnumerator:
                //case _sInitial:
                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }
    }
}
