namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that completes only when the token is canceled or when it's disposed.
        /// </summary>
        public static IAsyncEnumerable<T> Never<T>() => NeverAsyncEnumerable<T>.Singleton;

        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that completes only when the token is canceled or when it's disposed.
        /// </summary>
        public static IAsyncEnumerable<T> Never<T>(T sample) => NeverAsyncEnumerable<T>.Singleton;

        private sealed class NeverAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            public static NeverAsyncEnumerable<T> Singleton { get; } = new NeverAsyncEnumerable<T>();
            private NeverAsyncEnumerable() { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(token);

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sPulling = 1;
                private const int _sFinal = 2;

                private CoAwaiterCompleter<bool> _ccs = CoAwaiterCompleter<bool>.Init();
                private CancellationTokenRegistration _ctr;
                private int _state;
                private Exception _error;

                public Enumerator(CancellationToken token)
                {
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current => default;

                public ICoAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext = false)
                {
                    _ccs.Reset(continueOnCapturedContext);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            _ccs.SetCompleted(_error, false);
                            break;
                        default: // Pulling???
                            _state = state;
                            throw new Exception(_state + "???");
                    }

                    return _ccs.Awaiter;
                }

                public Task DisposeAsync()
                {
                    Cancel(null);
                    return Task.CompletedTask;
                }

                private void Cancel(OperationCanceledException error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = (Exception)error ?? new ObjectDisposedException(nameof(IAsyncEnumerator<T>));
                            _state = _sFinal;
                            _ctr.Dispose();
                            break;
                        case _sPulling:
                            _error = (Exception)error ?? new ObjectDisposedException(nameof(IAsyncEnumerator<T>));
                            _state = _sFinal;
                            _ctr.Dispose();
                            _ccs.SetCompleted(error, false);
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            break;
                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
        }
    }
}
