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
    /// Expose a <see cref="IAsyncEnumerable{T}"/> as a <see cref="ILinxObservable{T}"/>.
    /// </summary>
    /// <remarks>
    /// Must be enumerated by a consumer that processes items synchronously, or the sequence will fail.
    /// If this cannot be guaranteed, use buffering or lossy operators that do guarantee it.
    /// </remarks>
    public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(this ILinxObservable<T> source)
        => new ObservableIterator<T>(source);

    private sealed class ObservableIterator<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
    {
        private const int _sEnumerator = 0;
        private const int _sIdle1st = 1;
        private const int _sIdle = 2;
        private const int _sMoving = 3;
        private const int _sCanceled = 4;
        private const int _sCompleted = 5;
        private const int _sDisposing = 6;
        private const int _sDisposingMoving = 7;
        private const int _sDisposed = 8;

        private readonly ILinxObservable<T> _source;

        private readonly CancellationTokenSource _cts = new();
        private readonly ManualResetValueTaskCompleter<bool> _vtcMoving = new();
        private AsyncTaskMethodBuilder _atmbDisposed = AsyncTaskMethodBuilder.Create();
        private CancellationTokenRegistration _ctr;
        private int _state;
        private T? _current;
        private Exception? _error;

        public ObservableIterator(ILinxObservable<T> source)
            => _source = source ?? throw new ArgumentNullException(nameof(source));

        private ObservableIterator(ObservableIterator<T> parent)
            => _source = parent._source;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
        {
            if (Atomic.CompareExchange(ref _state, _sIdle1st, _sEnumerator) != _sEnumerator)
                return new ObservableIterator<T>(this).GetAsyncEnumerator(token);

            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token), false));

            return this;
        }

        public T Current => _current!;

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

                case _sIdle:
                    _vtcMoving.Reset();
                    _state = _sMoving;
                    return _vtcMoving.ValueTask;

                case _sCanceled:
                case _sDisposing:
                    _vtcMoving.Reset();
                    _state = _sDisposingMoving;
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
                case _sEnumerator:
                    _state = _sEnumerator;
                    throw new InvalidOperationException();

                case _sIdle1st:
                case _sIdle:
                    _error = error;
                    _state = disposing ? _sDisposing : _sCanceled;
                    _ctr.Dispose();
                    _cts.Cancel();
                    break;

                case _sMoving:
                    _error = error;
                    _state = _sDisposingMoving;
                    _ctr.Dispose();
                    _cts.Cancel();
                    break;

                case _sCanceled:
                    _state = disposing ? _sDisposing : _sCanceled;
                    break;

                case _sCompleted:
                    if (disposing)
                    {
                        _current = default;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.Reset();
                        _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    }
                    else
                        _state = _sCompleted;
                    break;

                case _sDisposing:
                case _sDisposingMoving:
                case _sDisposed:
                    _state = state;
                    break;
            }
        }

        private async void Produce()
        {
            Exception? error = null;
            try
            {
                await LinxAsync.Yield();
                await _source.Subscribe(Yield, _cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sIdle1st:
                    case _sIdle:
                        _error = error;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _cts.Cancel();
                        break;

                    case _sMoving:
                        _current = default;
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _cts.Cancel();
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(error, false);
                        break;

                    case _sCanceled:
                        _state = _sCompleted;
                        break;

                    case _sDisposing:
                        _current = default;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.Reset();
                        _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                        break;

                    case _sDisposingMoving:
                        _current = default;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _vtcMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                        break;

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
                case _sIdle:
                    _state = _sIdle;
                    throw new Exception("Consumer not ready.");

                case _sMoving:
                    _current = item;
                    _state = _sIdle;
                    _vtcMoving.SetResult(true);
                    return true;

                case _sCanceled:
                case _sCompleted:
                case _sDisposing:
                case _sDisposingMoving:
                case _sDisposed:
                    _state = state;
                    return false;

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }
    }
}
