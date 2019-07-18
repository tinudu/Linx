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

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(_producer, token);

            [DebuggerNonUserCode]
            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private const int _sInitial = 0; // Produce() not called yet
                private const int _sAccepting = 1; // pending MoveNextAsync()
                private const int _sEmitting = 2; // pending Yield()
                private const int _sCompleted = 3; // Produce completed
                private const int _sDisposing = 4; // but not Completed
                private const int _sDisposingAccepting = 5; // Disposing and Accepting
                private const int _sDisposed = 6; // Disposed

                private readonly ProducerDelegate<T> _producer;
                private readonly CancellationToken _token;
                private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
                private readonly ManualResetValueTaskSource<bool> _tsYield = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = default;
                private int _state;
                private Exception _error;

                public Enumerator(ProducerDelegate<T> producer, CancellationToken token)
                {
                    _producer = producer;
                    _token = token;
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    _tsAccepting.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial: // first call
                            _state = _sAccepting;
                            Produce();
                            break;

                        case _sEmitting:
                            _state = _sAccepting;
                            if (_token.IsCancellationRequested) _tsYield.SetException(new OperationCanceledException(_token));
                            else _tsYield.SetResult(true);
                            break;

                        case _sCompleted:
                            Current = default;
                            _state = _sCompleted;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        case _sDisposing:
                            _state = _sDisposingAccepting;
                            break;

                        case _sDisposed:
                            _state = _sDisposed;
                            _tsAccepting.SetException(AsyncEnumeratorDisposedException.Instance);
                            break;

                        default: // Accepting, DisposingAccepting ???
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
                            _state = _sDisposed;
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            _state = _sDisposingAccepting;
                            break;

                        case _sEmitting:
                            _state = _sDisposing;
                            _tsYield.SetResult(false);
                            break;

                        case _sCompleted:
                            Current = default;
                            _error = null;
                            _state = _sDisposed;
                            _atmbDisposed.SetResult();
                            break;

                        default: // disposing or disposed
                            _state = state;
                            break;
                    }

                    return new ValueTask(_atmbDisposed.Task);
                }

                private ValueTask<bool> Yield(T element)
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
                                _tsAccepting.SetResult(true);
                            }
                            break;

                        case _sCompleted:
                        case _sDisposing:
                        case _sDisposingAccepting:
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

                private async void Produce()
                {
                    try
                    {
                        _token.ThrowIfCancellationRequested();
                        await _producer(Yield, _token).ConfigureAwait(false);
                    }
                    catch (Exception ex) { _error = ex; }

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sAccepting:
                            Current = default;
                            _state = _sCompleted;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                            break;

                        case _sEmitting:
                            _state = _sCompleted;
                            _tsYield.SetResult(false);
                            break;

                        case _sDisposing:
                            Current = default;
                            _error = null;
                            _state = _sDisposed;
                            _atmbDisposed.SetResult();
                            break;

                        case _sDisposingAccepting:
                            Current = default;
                            _error = null;
                            _state = _sDisposed;
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetException(AsyncEnumeratorDisposedException.Instance);
                            break;

                        default: // Initial, Completed, Disposed ???
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
        }
    }
}
