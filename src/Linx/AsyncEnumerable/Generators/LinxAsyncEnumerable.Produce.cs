namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

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
                private const int _sAccepting = 1;
                private const int _sEmitting = 2;
                private const int _sCanceling = 3;
                private const int _sCancelingAccepting = 4;
                private const int _sFinal = 5;

                private readonly ProduceEnumerable<T> _source;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource _tsEmitting = new ManualResetValueTaskSource();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private ErrorHandler _eh = ErrorHandler.Init();
                private int _state;

                public Enumerator(ProduceEnumerable<T> source, CancellationToken token)
                {
                    _source = source;
                    if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            Produce();
                            break;
                        case _sEmitting:
                            _state = _sAccepting;
                            _tsEmitting.SetResult();
                            break;
                        case _sCanceling:
                            _state = _sCancelingAccepting;
                            break;
                        case _sFinal:
                            Current = default;
                            _state = _sFinal;
                            _tsAccepting.SetExceptionOrResult(_eh.Error, false);
                            break;
                        default: // Accepting, CancelingAccepting???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tsAccepting.Task;
                }

                public ValueTask DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return new ValueTask(_atmbDisposed.Task);
                }

                private ValueTask OnNext(T value)
                {
                    _tsEmitting.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Current = value;
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                            break;
                        case _sCanceling:
                        case _sCancelingAccepting:
                        case _sFinal:
                            _state = state;
                            _tsEmitting.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;
                        default: // Initial, Emitting???
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tsEmitting.Task;
                }

                private void Cancel(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _eh.SetExternalError(error);
                            _eh.SetInternalError(new OperationCanceledException(_eh.InternalToken));
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            break;
                        case _sAccepting:
                            _eh.SetExternalError(error);
                            _state = _sCancelingAccepting;
                            _eh.Cancel();
                            break;
                        case _sEmitting:
                            _eh.SetExternalError(error);
                            _state = _sCanceling;
                            _eh.Cancel();
                            _tsEmitting.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;
                        case _sCanceling:
                        case _sCancelingAccepting:
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
                        await _source._producer(OnNext, _eh.InternalToken).ConfigureAwait(false);
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                    var state = Atomic.Lock(ref _state);
                    if (error != null) _eh.SetInternalError(error);
                    switch (state)
                    {
                        case _sAccepting:
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            Current = default;
                            _tsAccepting.SetExceptionOrResult(_eh.Error, false);
                            break;

                        case _sEmitting:
                            _state = _sFinal;
                            _eh.Cancel();
                            _atmbDisposed.SetResult();
                            _tsEmitting.SetException(new OperationCanceledException(_eh.InternalToken));
                            break;

                        case _sCanceling:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        case _sCancelingAccepting:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            Current = default;
                            _tsAccepting.SetExceptionOrResult(_eh.Error, false);
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
