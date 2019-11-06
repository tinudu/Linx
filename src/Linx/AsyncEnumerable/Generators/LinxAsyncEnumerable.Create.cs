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
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly GeneratorDelegate<T> _generator;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly ManualResetValueTaskSource<bool> _tsEmitting = new ManualResetValueTaskSource<bool>();
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private int _state;
            private Exception _error;

            public GeneratorEnumerator(GeneratorDelegate<T> generator, CancellationToken token)
            {
                Debug.Assert(generator != null);
                _generator = generator;
                _token = token;
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

                    case _sCompleted:
                    case _sFinal:
                        Current = default;
                        _state = state;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = AsyncEnumeratorDisposedException.Instance;
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = AsyncEnumeratorDisposedException.Instance;
                        _state = _sCompleted;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sEmitting:
                        _error = AsyncEnumeratorDisposedException.Instance;
                        _state = _sCompleted;
                        _tsEmitting.SetResult(false);
                        break;

                    case _sCompleted:
                    case _sFinal:
                        _state = state;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                return new ValueTask(_atmbDisposed.Task);
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

                    case _sCompleted:
                    case _sFinal:
                        _state = state;
                        _tsEmitting.SetResult(false);
                        break;

                    default: // initial, emitting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsEmitting.Task;
            }

            private async void Produce()
            {
                Exception error = null;
                try
                {
                    _token.ThrowIfCancellationRequested();
                    await _generator(Yield, _token).ConfigureAwait(false);
                }
                catch (Exception ex) { error = ex; }

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        _error = error;
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(error, false);
                        _atmbDisposed.SetResult();
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sFinal;
                        _tsEmitting.SetResult(false);
                        _atmbDisposed.SetResult();
                        break;

                    case _sCompleted:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default: // Initial, Final???
                        _state = state;
                        throw new Exception(state + "???");
                }
            }
        }
    }
}
