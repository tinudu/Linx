namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by it's <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
        /// </summary>
        public static IAsyncEnumerable<T> Create<T>(Func<CancellationToken, IAsyncEnumerator<T>> getAsyncEnumerator, [CallerMemberName] string name = null)
        {
            if (getAsyncEnumerator == null) throw new ArgumentNullException(nameof(getAsyncEnumerator));
            return new AnonymousAsyncEnumerable<T>(getAsyncEnumerator, name);
        }

        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="GeneratorDelegate{T}"/> coroutine.
        /// </summary>
        public static IAsyncEnumerable<T> Create<T>(GeneratorDelegate<T> generator, [CallerMemberName] string name = null)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            return new AnonymousAsyncEnumerable<T>(token => new GeneratorEnumerator<T>(generator, token), name);
        }

        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="GeneratorDelegate{T}"/> coroutine.
        /// </summary>
        public static IAsyncEnumerable<T> Create<T>(T _, GeneratorDelegate<T> generator, [CallerMemberName] string name = null)
            => Create(generator, name);

        private sealed class GeneratorEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sDisposed = 3;
            private const int _sFinal = 4;

            private readonly GeneratorDelegate<T> _generator;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new();
            private readonly ManualResetValueTaskSource<bool> _tsEmitting = new();
            private AsyncTaskMethodBuilder _atmbDisposed = new();
            private CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;

            public GeneratorEnumerator(GeneratorDelegate<T> generator, CancellationToken token)
            {
                Debug.Assert(generator != null);
                _generator = generator;
                _token = token;

                if (token.CanBeCanceled)
                    _ctr = token.Register(() => Dispose(new OperationCanceledException(token)));
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
                        _tsEmitting.SetResult(true);
                        break;

                    case _sDisposed:
                    case _sFinal:
                        Current = default;
                        _state = state;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting???
                        _state = state;
                        Debug.Fail(state + "???");
                        break;
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                Dispose(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void Dispose(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = error;
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sDisposed;
                        _ctr.Dispose();
                        _tsEmitting.SetResult(false);
                        break;

                    default:
                        Debug.Assert(state == _sDisposed || state == _sFinal);
                        _state = state;
                        break;
                }
            }

            private ValueTask<bool> Yield(T value)
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

                    default:
                        Debug.Assert(state == _sDisposed || state == _sFinal);
                        _state = state;
                        _tsEmitting.SetResult(false);
                        break;
                }

                return _tsEmitting.Task;
            }

            private async void Produce()
            {
                Exception error = null;
                try { await _generator(Yield, _token).ConfigureAwait(false); }
                catch (Exception ex) { error = ex; }

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        _error = error;
                        Current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsAccepting.SetExceptionOrResult(error, false);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsEmitting.SetResult(false);
                        break;

                    default:
                        Debug.Assert(state == _sDisposed);
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;
                }
            }
        }
    }
}
