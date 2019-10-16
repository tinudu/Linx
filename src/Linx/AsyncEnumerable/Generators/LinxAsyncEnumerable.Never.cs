namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using TaskSources;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that completes only when the token is canceled or when it's disposed.
        /// </summary>
        public static IAsyncEnumerable<T> Never<T>() => NeverAsyncEnumerable<T>.Singleton;

        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that completes only when the token is canceled or when it's disposed.
        /// </summary>
        public static IAsyncEnumerable<T> Never<T>(T _) => NeverAsyncEnumerable<T>.Singleton;

        private sealed class NeverAsyncEnumerable<T> : AsyncEnumerableBase<T>
        {
            public static NeverAsyncEnumerable<T> Singleton { get; } = new NeverAsyncEnumerable<T>();
            private NeverAsyncEnumerable() { }

            public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(token);

            public override string ToString() => "Never";

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1;
                private const int _sFinal = 2;

                private readonly ManualResetValueTaskSource<bool> _tp = new ManualResetValueTaskSource<bool>();
                private readonly CancellationTokenRegistration _ctr;
                private int _state;
                private Exception _error;

                public Enumerator(CancellationToken token)
                {
                    if (token.CanBeCanceled) _ctr = token.Register(() => Cancel(new OperationCanceledException(token)));
                }

                public T Current => default;

                public ValueTask<bool> MoveNextAsync()
                {
                    _tp.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            break;
                        case _sFinal:
                            _state = _sFinal;
                            _tp.SetException(_error);
                            break;
                        default: // Accepting???
                            _state = state;
                            throw new Exception(_state + "???");
                    }

                    return _tp.Task;
                }

                public ValueTask DisposeAsync()
                {
                    Cancel(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(Task.CompletedTask);
                }

                public override string ToString() => "Never";

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
                        case _sAccepting:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _tp.SetException(error);
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
