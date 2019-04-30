namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskProviders;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProducerDelegate{T}"/> coroutine.
        /// </summary>
        public static IAsyncEnumerable<T> Produce<T>(ProducerDelegate<T> producer)
        {
            if (producer == null) throw new ArgumentNullException(nameof(producer));
            return new ProduceEnumerable<T>(producer);
        }

        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="ProducerDelegate{T}"/> coroutine.
        /// </summary>
        public static IAsyncEnumerable<T> Produce<T>(T sample, ProducerDelegate<T> producer)
        {
            if (producer == null) throw new ArgumentNullException(nameof(producer));
            return new ProduceEnumerable<T>(producer);
        }

        [DebuggerNonUserCode]
        private sealed class ProduceEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly ProducerDelegate<T> _producer;

            public ProduceEnumerable(ProducerDelegate<T> producer) => _producer = producer;

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
                private ManualResetTaskProvider<bool> _tpMoveNext = new ManualResetTaskProvider<bool>();
                private ManualResetTaskProvider _tpOnNext = new ManualResetTaskProvider();
                private ErrorHandler _eh = ErrorHandler.Init();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;

                public Enumerator(ProduceEnumerable<T> enumerable, CancellationToken token)
                {
                    _enumerable = enumerable;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tpMoveNext.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            Produce();
                            break;
                        case _sPushing:
                            _state = _sPulling;
                            _tpOnNext.SetResult();
                            break;
                        case _sCanceling:
                            _state = _sCancelingPulling;
                            break;
                        case _sFinal:
                            Current = default;
                            _state = _sFinal;
                            _tpMoveNext.SetExceptionOrResult(_eh.Error, false);
                            break;
                        default: // Pulling, CancelingPulling
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tpMoveNext.Task;
                }

                public ValueTask DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return new ValueTask(_atmbDisposed.Task);
                }

                private ValueTask OnNext(T value)
                {
                    _tpOnNext.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sPulling:
                            Current = value;
                            _state = _sPushing;
                            _tpMoveNext.SetResult(true);
                            break;
                        case _sCanceling:
                        case _sCancelingPulling:
                            _state = state;
                            Atomic.WaitCanceled(_eh.InternalToken);
                            _tpOnNext.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;
                        default: // Initial, Pushing, Final ???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tpOnNext.Task;
                }

                private void Cancel(Exception error)
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
                            _tpOnNext.SetException(new OperationCanceledException(_eh.InternalToken));
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
                        await _enumerable._producer(OnNext, _eh.InternalToken).ConfigureAwait(false);
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
                            _tpMoveNext.SetExceptionOrResult(_eh.Error, false);
                            break;

                        case _sPushing:
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            _tpOnNext.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;

                        case _sCanceling:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        case _sCancelingPulling:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            Current = default;
                            _tpMoveNext.SetExceptionOrResult(_eh.Error, false);
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
