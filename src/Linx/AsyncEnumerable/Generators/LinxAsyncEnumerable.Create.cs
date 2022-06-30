using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Linx.Tasking;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Create a <see cref="IAsyncEnumerable{T}"/> defined by it's <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
    /// </summary>
    public static IAsyncEnumerable<T> Create<T>(Func<CancellationToken, IAsyncEnumerator<T>> getAsyncEnumerator)
    {
        if (getAsyncEnumerator is null) throw new ArgumentNullException(nameof(getAsyncEnumerator));
        return new AnonymousAsyncEnumerable<T>(getAsyncEnumerator);
    }

    /// <summary>
    /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProduceAsyncDelegate{T}"/> coroutine.
    /// </summary>
    public static IAsyncEnumerable<T> Create<T>(ProduceAsyncDelegate<T> produceAsync, [CallerMemberName] string? displayName = default)
    {
        if (produceAsync == null) throw new ArgumentNullException(nameof(produceAsync));
        return new CoroutineIterator<T>(produceAsync, displayName);
    }

    /// <summary>
    /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProduceAsyncDelegate{T}"/> coroutine.
    /// </summary>
    public static IAsyncEnumerable<T> Create<T>(T _, ProduceAsyncDelegate<T> produceAsync, [CallerMemberName] string? displayName = default)
    {
        if (produceAsync == null) throw new ArgumentNullException(nameof(produceAsync));
        return new CoroutineIterator<T>(produceAsync, displayName);
    }

    private sealed class AnonymousAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Func<CancellationToken, IAsyncEnumerator<T>> _getEnumerator;

        public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator)
        {
            if (getEnumerator is null) throw new ArgumentNullException(nameof(getEnumerator));
            _getEnumerator = getEnumerator;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => _getEnumerator(token);
    }

    private sealed class CoroutineIterator<T> : IAsyncEnumerator<T>, IAsyncEnumerable<T>
    {
        private const int _sEnumerator = 0; // GetEnumeratorAsync has not been called
        private const int _sYielding = 1; // pending YieldAsync
        private const int _sMoving = 2; // pending MoveNextAsync
        private const int _sCanceled = 3; // error but not completed, no pending xxxAsync
        private const int _sCompleted = 4; // Produce completed with or without error, no pending xxxAsync
        private const int _sDisposing = 5; // error, pending DisposeAsync
        private const int _sDisposingMoving = 6; // error, pending MoveNextAsync
        private const int _sDisposed = 7; // final state

        private readonly ProduceAsyncDelegate<T> _produceAsync;
        private readonly string? _displayName;

        private readonly ManualResetValueTaskSource<bool> _tsMoving = new();
        private readonly ManualResetValueTaskSource<bool> _tsYielding = new();
        private AsyncTaskMethodBuilder _atmbDisposed;
        private CancellationTokenRegistration _ctr;
        private int _state;
        private T? _current;
        private Exception? _error;

        public CoroutineIterator(ProduceAsyncDelegate<T> produceAsync, string? displayName)
        {
            if (produceAsync is null) throw new ArgumentNullException(nameof(produceAsync));

            _produceAsync = produceAsync;
            _displayName = displayName ?? base.ToString();
        }

        private CoroutineIterator(CoroutineIterator<T> parent)
        {
            _produceAsync = parent._produceAsync;
            _displayName = parent._displayName;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
        {
            if (Atomic.CompareExchange(ref _state, _sYielding, _sEnumerator) != _sEnumerator) // already enumerating
                return new CoroutineIterator<T>(this).GetAsyncEnumerator(token);

            Produce(token);
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token), false));

            return this;
        }

        /// <inheritdoc />
        public T Current => _current!;

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sYielding:
                    _tsMoving.Reset();
                    _state = _sMoving;
                    _tsYielding.SetResult(true);
                    break;

                case _sCanceled:
                case _sDisposing:
                    _tsMoving.Reset();
                    _state = _sDisposingMoving;
                    break;

                case _sCompleted:
                    _current = default;
                    _tsMoving.Reset();
                    _state = _sDisposed;
                    _atmbDisposed.SetResult();
                    _tsMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    break;

                case _sDisposed:
                    _state = _sDisposed;
                    break;

                case _sEnumerator:
                case _sMoving:
                case _sDisposingMoving:
                    _state = state;
                    throw new InvalidOperationException();

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
            return _tsMoving.ValueTask;
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
                case _sYielding:
                    _error = error;
                    _state = disposing ? _sDisposing : _sCanceled;
                    _ctr.Dispose();
                    _tsYielding.SetResult(false);
                    break;

                case _sMoving:
                    _error = error;
                    _state = _sDisposingMoving;
                    _ctr.Dispose();
                    break;

                case _sCanceled:
                    _state = disposing ? _sDisposing : _sCanceled;
                    break;

                case _sCompleted:
                    if (disposing)
                    {
                        _current = default;
                        _tsMoving.Reset();
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _tsMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                    }
                    else
                        _state = _sCompleted;
                    break;

                case _sDisposing:
                case _sDisposingMoving:
                case _sDisposed:
                    _state = state;
                    break;

                case _sEnumerator:
                    _state = _sEnumerator;
                    throw new InvalidOperationException();

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        private async void Produce(CancellationToken token)
        {
            Exception? error = null;
            try
            {
                if (!await _tsYielding.ValueTask.ConfigureAwait(false))
                    return;

                await _produceAsync(YieldAsync, token).ConfigureAwait(false);
            }
            catch (Exception ex) { error = ex; }
            finally // go completed or disposed
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sYielding:
                        _error = error;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _tsYielding.SetResult(false);
                        break;

                    case _sMoving:
                        _current = default;
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsMoving.SetExceptionOrResult(error, false);
                        break;

                    case _sCanceled:
                        _state = _sCompleted;
                        break;

                    case _sDisposing:
                        _current = default;
                        _tsMoving.Reset();
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _tsMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                        break;

                    case _sDisposingMoving:
                        _current = default;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        _tsMoving.SetExceptionOrResult(Linx.Clear(ref _error), false);
                        break;

                    default:
                        _state = state;
                        Debug.Fail(state + "???");
                        break;
                }
            }
        }

        private ValueTask<bool> YieldAsync(T item)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sMoving:
                    _current = item;
                    _tsYielding.Reset();
                    _state = _sYielding;
                    _tsMoving.SetResult(true);
                    return _tsYielding.ValueTask;

                case _sCanceled:
                case _sCompleted:
                case _sDisposing:
                case _sDisposingMoving:
                case _sDisposed:
                    _state = state;
                    return new(false);

                case _sYielding:
                    _state = _sYielding;
                    throw new InvalidOperationException();

                default:
                    _state = state;
                    throw new InvalidOperationException(state + "???");
            }
        }

        public override string? ToString() => _displayName;
    }
}
