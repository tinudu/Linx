namespace Linx.Reactive
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProduceDelegate{T}"/> coroutine.
        /// </summary>
        public static IAsyncEnumerable<T> Produce<T>(ProduceDelegate<T> produce)
        {
            if (produce == null) throw new ArgumentNullException(nameof(produce));
            return new ProduceEnumerable<T>(produce);
        }

        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProduceDelegate{T}"/> coroutine.
        /// </summary>
        public static IAsyncEnumerable<T> Produce<T>(T sample, ProduceDelegate<T> produce)
        {
            if (produce == null) throw new ArgumentNullException(nameof(produce));
            return new ProduceEnumerable<T>(produce);
        }

        [DebuggerNonUserCode]
        private sealed class ProduceEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly ProduceDelegate<T> _produce;

            public ProduceEnumerable(ProduceDelegate<T> produce) => _produce = produce;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            [DebuggerNonUserCode]
            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sPushing = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingPulling = 4;
                private const int _sFinal = 5;

                private readonly ProduceEnumerable<T> _enumerable;
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private CoroutineCompletionSource<bool> _ccsMoveNext = CoroutineCompletionSource<bool>.Init();
                private CoroutineCompletionSource _ccsOnNext = CoroutineCompletionSource.Init();
                private int _state;

                public Enumerator(ProduceEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext = false)
                {
                    _ccsMoveNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            Produce();
                            break;
                        case _sPushing:
                            _state = _sPulling;
                            _ccsOnNext.SetCompleted(null);
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            Current = default;
                            _state = _sFinal;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _ccsMoveNext.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return _atmbDisposed.Task;
                }

                private ICoroutineAwaiter OnNext(T value, bool continueOnCapturedContext)
                {
                    _ccsOnNext.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Current = value;
                            _state = _sPushing;
                            _ccsMoveNext.SetCompleted(null, true);
                            break;
                        case _sCanceling:
                        case _sCancelingPulling:
                            _state = state;
                            Atomic.WaitCanceled(_eh.InternalToken);
                            _ccsOnNext.SetCompleted(new OperationCanceledException(_eh.InternalToken));
                            break;
                        default: // Initial, Pushing, Final ???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _ccsOnNext.Awaiter;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;
                        case _sPulling:
                            _eh.SetExternalError(error);
                            _state = _sCancelingPulling;
                            _eh.Cancel();
                            break;
                        case _sPushing:
                            _eh.SetExternalError(error);
                            _state = _sCanceling;
                            _eh.Cancel();
                            _ccsOnNext.SetCompleted(new OperationCanceledException(_eh.InternalToken));
                            break;
                        case _sCanceling:
                        case _sCancelingPulling:
                        case _sFinal:
                            _state = state;
                            break;
                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private async void Produce()
                {
                    Exception error;
                    try
                    {
                        await _enumerable._produce(OnNext, _eh.InternalToken).ConfigureAwait(false);
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    var state = Atomic.Lock(ref _state);
                    _eh.SetInternalError(error);
                    switch (state)
                    {
                        case _sPulling:
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;

                        case _sPushing:
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            _ccsOnNext.SetCompleted(new OperationCanceledException(_eh.InternalToken));
                            break;

                        case _sCanceling:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        case _sCancelingPulling:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            Current = default;
                            _ccsMoveNext.SetCompleted(_eh.Error, false);
                            break;

                        default: // Initial, Final??
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
        }
    }
}
