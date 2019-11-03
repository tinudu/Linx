namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncEnumerable;
    using TaskSources;

    partial class LinxObservable
    {
        /// <summary>
        /// Buffers items in case the consumer is slower than the generator.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return LinxAsyncEnumerable.Create(token => new BufferAllEnumerator<T>(source, token));
        }

        private sealed class BufferAllEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sError = 3;
            private const int _sLast = 4;
            private const int _sFinal = 5;

            private readonly ILinxObservable<T> _source;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly Queue<T> _queue = new Queue<T>();
            private readonly Observer _observer;
            private int _state;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbDisposed = default;

            public BufferAllEnumerator(ILinxObservable<T> source, CancellationToken token)
            {
                Debug.Assert(source != null);

                _source = source;
                _token = token;
                _observer = new Observer(this);
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
                        _source.SafeSubscribe(_observer);
                        break;

                    case _sEmitting:
                        if (_queue.Count > 0)
                        {
                            Current = _queue.Dequeue();
                            if (_queue.Count == 0) // consumer now faster than producer
                                try { _queue.TrimExcess(); }
                                catch {/**/}
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sError:
                        Current = default;
                        _state = _sError;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sLast:
                        Debug.Assert(_error == null && _queue.Count > 0);
                        Current = _queue.Dequeue();
                        if (_queue.Count == 0)
                        {
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sLast;
                        _tsAccepting.SetResult(true);
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
                        _tsAccepting.SetExceptionOrResult(_error, false);
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsAccepting.Task;
            }

            public ValueTask DisposeAsync()
            {
                OnError(AsyncEnumeratorDisposedException.Instance);
                return new ValueTask(_atmbDisposed.Task);
            }

            private void OnError(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        Current = default;
                        _error = error;
                        _state = _sError;
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sError;
                        _queue.Clear();
                        break;

                    case _sLast:
                        _error = error;
                        _state = _sFinal;
                        _queue.Clear();
                        _atmbDisposed.SetResult();
                        break;

                    default: // Error, Final
                        _state = state;
                        break;
                }
            }

            private sealed class Observer : ILinxObserver<T>
            {
                private readonly BufferAllEnumerator<T> _e;
                public Observer(BufferAllEnumerator<T> enumerator) => _e = enumerator;

                public CancellationToken Token => _e._token;

                public bool OnNext(T value)
                {
                    var state = Atomic.Lock(ref _e._state);
                    switch (state)
                    {
                        case _sAccepting:
                            _e.Current = value;
                            _e._state = _sEmitting;
                            _e._tsAccepting.SetResult(true);
                            return true;

                        case _sEmitting:
                            try
                            {
                                _e._queue.Enqueue(value);
                                _e._state = _sEmitting;
                            }
                            catch (Exception ex)
                            {
                                _e._state = _sEmitting;
                                _e.OnError(ex);
                                return false;
                            }
                            return true;

                        case _sError:
                        case _sLast:
                        case _sFinal:
                            _e._state = state;
                            return false;

                        default: // Initial???
                            _e._state = state;
                            throw new Exception(state + "???");
                    }
                }

                public void OnError(Exception error)
                {
                    _e.OnError(error);
                    OnCompleted();
                }

                public void OnCompleted()
                {
                    var state = Atomic.Lock(ref _e._state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert(_e._queue.Count == 0);
                            _e.Current = default;
                            _e._state = _sFinal;
                            _e._atmbDisposed.SetResult();
                            _e._tsAccepting.SetResult(false);
                            break;

                        case _sEmitting when (_e._queue.Count > 0):
                            _e._state = _sLast;
                            break;

                        case _sEmitting:
                        case _sError:
                            _e._state = _sFinal;
                            _e._atmbDisposed.SetResult();
                            break;

                        case _sLast:
                        case _sFinal:
                            _e._state = state;
                            break;

                        default: // Initial
                            _e._state = state;
                            throw new Exception(state + "???");
                    }
                }
            }
        }
    }
}
