using Linx.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable
{
    internal sealed class CoroutineIterator<T> : IAsyncEnumerator<T>, IAsyncEnumerable<T>
    {
        private const int _sInitial = 0;
        private const int _sEmitting = 1;
        private const int _sAccepting = 2;
        private const int _sFinal = 3;

        private readonly ProduceAsyncDelegate<T> _produceAsync;
        private readonly string _displayName;
        private ManualResetValueTaskSource<bool> _tsAccepting;
        private ManualResetValueTaskSource<bool> _tsEmitting;
        private AsyncTaskMethodBuilder<bool> _atmbFinal;
        private AsyncTaskMethodBuilder _atmbDisposed;
        private CancellationTokenRegistration _ctr;
        private int _state;

        public CoroutineIterator(ProduceAsyncDelegate<T> produceAsync, string displayName)
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
            if (token.IsCancellationRequested)
                return new LinxAsyncEnumerable.ThrowIterator<T>(new OperationCanceledException(token));

            var state = Atomic.Lock(ref _state);
            if (state != _sInitial)
            {
                _state = state;
                return new CoroutineIterator<T>(this).GetAsyncEnumerator(token);
            }

            try
            {
                _tsAccepting = new();
                _tsEmitting = new();
                _tsEmitting.Reset();
                _state = _sEmitting;
                Produce(token);
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
                return this;
            }
            catch (Exception ex)
            {
                _state = Atomic.LockBit;
                Current = default;
                _tsAccepting = default;
                _tsEmitting = default;
                _atmbFinal = default;
                _atmbDisposed = default;
                _ctr = default;
                _state = _sFinal;
                _atmbFinal.SetException(ex);
                _atmbDisposed.SetResult();
                return new LinxAsyncEnumerable.ThrowIterator<T>(ex);
            }
        }

        /// <inheritdoc />
        public T Current { get; private set; }

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sInitial:
                    _state = _sInitial;
                    throw new InvalidOperationException("GetAsyncEnumerator was not called.");

                case _sEmitting:
                    _tsAccepting.Reset();
                    _state = _sAccepting;
                    _tsEmitting.SetResult(true);
                    return _tsAccepting.Task;

                case _sAccepting:
                    _state = _sAccepting;
                    throw new InvalidOperationException("MoveNextAsync is not reentrant.");

                case _sFinal:
                    Current = default;
                    _state = _sFinal;
                    return new(_atmbFinal.Task);

                default:
                    _state = state;
                    throw new Exception(state + "???");
            }
        }

        public ValueTask DisposeAsync()
        {
            SetFinal(AsyncEnumeratorDisposedException.Instance);
            Current = default;
            return new(_atmbDisposed.Task);
        }

        private void SetFinal(Exception error)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sInitial:
                    _state = _sInitial;
                    throw new InvalidOperationException();

                case _sEmitting:
                    _state = _sFinal;
                    _ctr.Dispose();
                    _atmbFinal.SetExceptionOrResult(error, false);
                    _tsEmitting.SetResult(false);
                    break;

                case _sAccepting:
                    Current = default;
                    _state = _sFinal;
                    _ctr.Dispose();
                    _atmbFinal.SetExceptionOrResult(error, false);
                    _tsAccepting.SetExceptionOrResult(error, false);
                    break;

                default: // _sFinal
                    _state = state;
                    break;
            }
        }

        private async void Produce(CancellationToken token)
        {
            Exception error = null;
            try
            {
                if (!await _tsEmitting.Task.ConfigureAwait(false))
                    return;

                await _produceAsync(YieldAsync, token).ConfigureAwait(false);
            }
            catch (Exception ex) { error = ex; }
            finally
            {
                _atmbDisposed.SetResult();
                SetFinal(error);
            }
        }

        private ValueTask<bool> YieldAsync(T item)
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sAccepting:
                    Current = item;
                    _tsEmitting.Reset();
                    _state = _sEmitting;
                    _tsAccepting.SetResult(true);
                    return _tsEmitting.Task;

                case _sFinal:
                    _state = _sFinal;
                    return new(false);

                default:
                    _state = state;
                    throw new InvalidOperationException(state + "???");
            }
        }

        public override string ToString() => _displayName;
    }
}
