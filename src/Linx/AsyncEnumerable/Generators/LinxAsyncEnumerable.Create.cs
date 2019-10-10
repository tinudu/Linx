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
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="GeneratorDelegate{T}"/> coroutine.
        /// </summary>
        /// <param name="generator">A <see cref="GeneratorDelegate{T}"/> that will emit elements.</param>
        /// <param name="name">A display name for the enumerable.</param>
        public static IAsyncEnumerable<T> Create<T>(GeneratorDelegate<T> generator, [CallerMemberName] string name = null)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            return new GeneratorEnumerable<T>(generator, name);
        }

        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by a <see cref="GeneratorDelegate{T}"/> coroutine.
        /// </summary>
        /// <param name="sample">Ignored. Helps with type inference.</param>
        /// <param name="generator">A <see cref="GeneratorDelegate{T}"/> that will emit elements.</param>
        /// <param name="name">A display name for the enumerable.</param>
        public static IAsyncEnumerable<T> Create<T>(T sample, GeneratorDelegate<T> generator, [CallerMemberName] string name = null)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            return new GeneratorEnumerable<T>(generator, name);
        }

        /// <summary>
        /// Create a <see cref="IAsyncEnumerable{T}"/> defined by it's <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
        /// </summary>
        public static IAsyncEnumerable<T> Create<T>(Func<CancellationToken, IAsyncEnumerator<T>> getAsyncEnumerator, [CallerMemberName] string name = null)
        {
            return new AnonymousAsyncEnumerable<T>(getAsyncEnumerator, name);
        }

        [DebuggerNonUserCode]
        private sealed class GeneratorEnumerable<T> : AsyncEnumerableBase<T>, IAsyncEnumerator<T>
        {
            private const int _sEnumerable = 0;
            private const int _sInitial = 1;
            private const int _sAccepting = 2;
            private const int _sEmitting = 3;
            private const int _sCompleted = 4;
            private const int _sFinal = 5;

            private readonly GeneratorDelegate<T> _generator;
            private readonly string _name;
            private int _state;
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
            private readonly ManualResetValueTaskSource<bool> _tsYield = new ManualResetValueTaskSource<bool>();
            private CancellationToken _token;
            private CancellationTokenRegistration _ctr;
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private Exception _error;

            public GeneratorEnumerable(GeneratorDelegate<T> generator, string name)
            {
                _generator = generator;
                _name = name ?? nameof(GeneratorEnumerable<T>);
            }

            public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var state = Atomic.Lock(ref _state);
                if (state != _sEnumerable)
                {
                    _state = state;
                    return new GeneratorEnumerable<T>(_generator, _name).GetAsyncEnumerator(token);
                }

                _state = _sInitial;
                _token = token;
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
                return this;
            }

            public override string ToString() => _name;

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsMoveNext.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerable:
                        _state = _sEnumerable;
                        _tsMoveNext.SetException(new InvalidOperationException());
                        break;

                    case _sInitial:
                        _state = _sAccepting;
                        Generate();
                        break;

                    case _sEmitting:
                        _state = _sAccepting;
                        _tsYield.SetResult(true);
                        break;

                    case _sCompleted:
                    case _sFinal:
                        Current = default;
                        _state = state;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        break;

                    default: // Accepting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsMoveNext.Task;
            }

            public async ValueTask DisposeAsync()
            {
                OnError(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                Current = default;
            }

            private void OnError(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerable:
                        _state = _sEnumerable;
                        throw new InvalidOperationException();

                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = error;
                        Current = default;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _tsMoveNext.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _tsYield.SetResult(false);
                        break;

                    case _sCompleted:
                    case _sFinal:
                        _state = state;
                        break;
                }
            }

            private ValueTask<bool> YieldAsync(T element)
            {
                _tsYield.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        Current = element;
                        _state = _sEmitting;
                        _tsMoveNext.SetResult(true);
                        break;

                    case _sCompleted:
                    case _sFinal:
                        _state = state;
                        _tsYield.SetResult(false);
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsYield.Task;
            }

            private async void Generate()
            {
                try { await _generator(YieldAsync, _token).ConfigureAwait(false); }
                catch (Exception ex) { OnError(ex); }

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        Current = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        break;

                    case _sEmitting:
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsYield.SetResult(false);
                        break;

                    case _sCompleted:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }
        }
    }
}
