namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Queueing;
    using TaskSources;

    partial class LinxObservable
    {
        /// <summary>
        /// Queueing <see cref="ILinxObservable{T}"/> to <see cref="IAsyncEnumerable{T}"/> enumerator.
        /// </summary>
        private sealed class QueueingEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sError = 3; // but not completed
            private const int _sCompleted = 4; // but not error
            private const int _sFinal = 5;

            private readonly ILinxObservable<T> _source;
            private readonly IQueue<T> _queue;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly Observer _observer;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbDisposed = default;

            public QueueingEnumerator(ILinxObservable<T> source, IQueue<T> queue, CancellationToken token)
            {
                Debug.Assert(source != null);
                Debug.Assert(queue != null);

                _source = source;
                _queue = queue;
                _token = token;
                _observer = new Observer(this);
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
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
                        if (_queue.IsEmpty)
                        {
                            Current = _queue.Dequeue();
                            if (_queue.IsEmpty) // consumer now faster than producer
                                try { _queue.TrimExcess(); }
                                catch {/**/}
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sError:
                        _state = _sError;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sCompleted:
                        if (_queue.IsEmpty)
                        {
                            Current = default;
                            _state = _sCompleted;
                            _tsAccepting.SetExceptionOrResult(_error, false);
                        }
                        else
                        {
                            Current = _queue.Dequeue();
                            _state = _sCompleted;
                            _tsAccepting.SetResult(true);
                        }
                        break;

                    case _sFinal:
                        Current = default;
                        _state = _sFinal;
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
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        Current = default;
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        Debug.Assert(_queue.IsEmpty);
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _queue.Clear();
                        break;

                    case _sCompleted:
                        if (_queue.IsEmpty)
                        {
                            _error = error;
                            _queue.Clear();
                        }
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    default: // Completed, Final
                        _state = state;
                        break;
                }
            }

            private sealed class Observer : ILinxObserver<T>
            {
                private readonly QueueingEnumerator<T> _e;
                public Observer(QueueingEnumerator<T> enumerator) => _e = enumerator;

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
                                return true;
                            }
                            catch (Exception ex)
                            {
                                _e._state = _sEmitting;
                                _e.OnError(ex);
                                return false;
                            }

                        default:
                            _e._state = state;
                            return false;
                    }
                }

                public void OnError(Exception error)
                {
                    _e.OnError(error ?? new ArgumentNullException(nameof(error)));
                    OnCompleted();
                }

                public void OnCompleted()
                {
                    var state = Atomic.Lock(ref _e._state);
                    switch (state)
                    {
                        case _sAccepting:
                            Debug.Assert(_e._queue.IsEmpty && _e._error == null);
                            _e.Current = default;
                            _e._state = _sFinal;
                            _e._ctr.Dispose();
                            _e._atmbDisposed.SetResult();
                            _e._tsAccepting.SetResult(false);
                            break;

                        case _sEmitting:
                            if (_e._queue.IsEmpty)
                            {
                                _e._state = _sFinal;
                                _e._ctr.Dispose();
                                _e._atmbDisposed.SetResult();
                            }
                            else
                                _e._state = _sCompleted;
                            break;

                        case _sError:
                            _e._state = _sFinal;
                            _e._atmbDisposed.SetResult();
                            break;

                        default:
                            _e._state = state;
                            break;
                    }
                }
            }
        }
    }
}