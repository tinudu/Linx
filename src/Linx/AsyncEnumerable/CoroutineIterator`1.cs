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

        private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
        private readonly ManualResetValueTaskSource<bool> _tsEmitting = new();
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
            var state = Atomic.Lock(ref _state);
            if (state != _sInitial)
            {
                _state = state;
                return new CoroutineIterator<T>(this).GetAsyncEnumerator(token);
            }

            _state = _sEmitting;
            Produce(token);
            if (token.CanBeCanceled)
                _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));

            return this;
        }

        /// <inheritdoc />
        public T Current { get; private set; }

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync()
        {
            var state = Atomic.Lock(ref _state);
            switch (state)
            {
                case _sEmitting:
                    _tsAccepting.Reset();
                    _state = _sAccepting;
                    _tsEmitting.SetResult(true);
                    return _tsAccepting.Task;

                case _sFinal:
                    Current = default;
                    _state = _sFinal;
                    return new(_atmbFinal.Task);

                case _sInitial:
                case _sAccepting:
                    _state = state;
                    throw new InvalidOperationException();

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
                SetFinal(error);
                _atmbDisposed.SetResult();
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
