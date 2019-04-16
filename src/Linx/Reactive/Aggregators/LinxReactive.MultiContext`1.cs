namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Multiple consumers/aggregators per subscription.
        /// </summary>
        private sealed class MultiContext<T> : IAsyncEnumerable<T>
        {
            private const int _sInitial = 0;
            private const int _sSubscribed = 1;
            private const int _sCanceling = 2;

            private readonly List<Enumerator> _enumerators;
            private int _state, _active, _index;
            private ErrorHandler _eh = ErrorHandler.Init();
            private AsyncTaskMethodBuilder _atmbDone = default;

            /// <summary>
            /// Initialize.
            /// </summary>
            /// <param name="capacity">Expected number of subscribers.</param>
            /// <param name="token">Token to cancel the whole operation.</param>
            public MultiContext(int capacity, CancellationToken token)
            {
                _enumerators = new List<Enumerator>(capacity);
                if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
            }

            public async Task<TAggregate> Aggregate<TAggregate>(AggregatorDelegate<T, TAggregate> aggregator)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                _active++;
                _state = _sInitial;

                TAggregate result;
                Exception error;
                try
                {
                    result = await aggregator(this, _eh.InternalToken).ConfigureAwait(false);
                    error = null;
                }
                catch (Exception ex)
                {
                    result = default;
                    error = ex;
                }

                HandleInternalError(error);
                return result;
            }

            public async Task Consume(ConsumerDelegate<T> consumer)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                _active++;
                _state = _sInitial;

                Exception error;
                try
                {
                    await consumer(this, _eh.InternalToken).ConfigureAwait(false);
                    error = null;
                }
                catch (Exception ex) { error = ex; }

                HandleInternalError(error);
            }

            public async Task SubscribeTo(IAsyncEnumerable<T> src)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                if (_enumerators.Count == 0)
                    _state = _sSubscribed;
                else
                {
                    _active++;
                    _state = _sSubscribed;
                    Exception error;
                    try
                    {
                        var ae = src.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                var current = ae.Current;
                                Debug.Assert(_index == 0);
                                bool @break;
                                while (true)
                                {
                                    state = Atomic.Lock(ref _state);
                                    if (_state != _sSubscribed)
                                    {
                                        @break = true;
                                        _state = state;
                                        break;
                                    }

                                    if (_index < _enumerators.Count)
                                    {
                                        var e = _enumerators[_index++];
                                        _state = _sSubscribed;
                                        await e.OnNext(current);
                                    }
                                    else
                                    {
                                        if (_index == 0)
                                            @break = true;
                                        else
                                        {
                                            @break = false;
                                            _index = 0;
                                        }
                                        _state = _sSubscribed;
                                        break;
                                    }
                                }

                                if (@break) break;
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                }
            }

            private void HandleInternalError(Exception error)
            {
                var s = Atomic.Lock(ref _state);
                _eh.SetInternalError(error);
                _state = 1;
                if (s == 0) _eh.Cancel();
            }

            IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private void Cancel(Exception error)
            {
                if (Atomic.TestAndSet(ref _state, 0, 1) != 0) return;
                _eh.SetExternalError(error);
                _eh.Cancel();
            }

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private readonly MultiContext<T> _context;
                public readonly CoCompletionSource<bool> AcPulling = CoCompletionSource<bool>.Init();
                public readonly T Current;
                public Exception Error;

                public Enumerator(MultiContext<T> context, CancellationToken token)
                {
                    _context = context;
                }

                public ICoAwaiter OnNext(T value) => CoAwaiter.Completed;

                T IAsyncEnumerator<T>.Current => Current;

                ICoAwaiter<bool> IAsyncEnumerator<T>.MoveNextAsync(bool continueOnCapturedContext)
                {
                    throw new NotImplementedException();
                }

                Task IAsyncEnumerator<T>.DisposeAsync()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
