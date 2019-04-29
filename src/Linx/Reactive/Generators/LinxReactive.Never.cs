namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Sources;

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

                private readonly ManualResetValueTaskSource<bool> _vts = new ManualResetValueTaskSource<bool>();
                private CancellationTokenRegistration _ctr;
                private int _state;
                private Exception _error;

                public Enumerator(CancellationToken token)
                {
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current => default;

                public ValueTask<bool> MoveNextAsync()
                {
                    _vts.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sPulling;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            _vts.SetException(_error);
                            break;
                        default: // Pulling???
                            _state = state;
                            throw new Exception(_state + "???");
                    }

                    return _vts.Task;
                }

                public ValueTask DisposeAsync()
                {
                    Cancel(ErrorHandler.EnumeratorDisposedException);
                    return new ValueTask(Task.CompletedTask);
                }

                private void Cancel(Exception error)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            break;
                        case _sPulling:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _vts.SetException(error);
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
