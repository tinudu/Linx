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

        [DebuggerNonUserCode]
        private sealed class GeneratorEnumerable<T> : AsyncEnumerableBase<T>, IAsyncEnumerator<T>
        {
            private const int _sEnumerable = 0; // it's an enumerable, not an enumerator
            private const int _sInitial = 1;
            private const int _sAccepting = 2; // pending MoveNextAsync()
            private const int _sEmitting = 3; // pending YieldAsync()
            private const int _sCompleted = 4; // Create() completed
            private const int _sDisposing = 5; // but not Completed
            private const int _sDisposed = 6;

            private readonly GeneratorDelegate<T> _generator;
            private readonly string _name;
            private int _state;

            public GeneratorEnumerable(GeneratorDelegate<T> generator, string name)
            {
                _generator = generator;
                _name = name ?? nameof(GeneratorEnumerable<T>);
            }

            private GeneratorEnumerable(GeneratorEnumerable<T> clonee, CancellationToken token)
            {
                _generator = clonee._generator;
                _name = clonee._name;
                _state = _sInitial;
                _token = token;
            }

            public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                var state = Atomic.Lock(ref _state);
                if (state == _sEnumerable)
                {
                    _token = token;
                    _state = _sInitial;
                    return this;
                }

                _state = state;
                return new GeneratorEnumerable<T>(this, token);
            }

            public override string ToString() => _name;

            // enumerator from here

            private CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
            private readonly ManualResetValueTaskSource<bool> _tsYield = new ManualResetValueTaskSource<bool>();
            private AsyncTaskMethodBuilder _atmbDisposed = default;
            private Exception _error;

            public T Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                _tsMoveNext.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerable:
                        _state = _sEnumerable;
                        throw new InvalidOperationException();

                    case _sInitial: // first call to MoveNextAsync
                        _state = _sAccepting;
                        Generate();
                        break;

                    case _sEmitting:
                        _state = _sAccepting;
                        if (_token.IsCancellationRequested) _tsYield.SetException(new OperationCanceledException(_token));
                        else _tsYield.SetResult(true);
                        break;

                    case _sCompleted:
                        Current = default;
                        _state = _sCompleted;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        break;

                    case _sDisposing:
                    case _sDisposed:
                        _state = state;
                        _tsMoveNext.SetException(AsyncEnumeratorDisposedException.Instance);
                        break;

                    default: // Accepting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsMoveNext.Task;
            }

            public ValueTask DisposeAsync()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerable:
                        _state = _sEnumerable;
                        throw new InvalidOperationException();

                    case _sInitial:
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        Current = default;
                        _state = _sDisposing;
                        _tsMoveNext.SetException(AsyncEnumeratorDisposedException.Instance);
                        break;

                    case _sEmitting:
                        Current = default;
                        _state = _sDisposing;
                        _tsYield.SetResult(false);
                        break;

                    case _sCompleted:
                        Current = default;
                        _error = null;
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        break;

                    case _sDisposing:
                    case _sDisposed:
                        _state = state;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                return new ValueTask(_atmbDisposed.Task);
            }

            private ValueTask<bool> YieldAsync(T element)
            {
                _tsYield.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        if (_token.IsCancellationRequested)
                        {
                            _state = _sAccepting;
                            _tsYield.SetException(new OperationCanceledException(_token));
                        }
                        else
                        {
                            Current = element;
                            _state = _sEmitting;
                            _tsMoveNext.SetResult(true);
                        }
                        break;

                    case _sCompleted:
                    case _sDisposing:
                    case _sDisposed:
                        _state = state;
                        _tsYield.SetResult(false);
                        break;

                    default: // Initial, Emitting???
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsYield.Task;
            }

            private async void Generate()
            {
                Exception error;
                try
                {
                    _token.ThrowIfCancellationRequested();
                    await _generator(YieldAsync, _token).ConfigureAwait(false);
                    error = null;
                }
                catch (Exception ex) { error = ex; }

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sAccepting:
                        Current = default;
                        _error = error;
                        _state = _sCompleted;
                        _tsMoveNext.SetExceptionOrResult(_error, false);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sCompleted;
                        _tsYield.SetResult(false);
                        break;

                    case _sDisposing:
                        _state = _sDisposed;
                        _atmbDisposed.SetResult();
                        break;

                    default: // Initial, Completed, Disposed ???
                        _state = state;
                        throw new Exception(state + "???");
                }
            }
        }
    }
}
