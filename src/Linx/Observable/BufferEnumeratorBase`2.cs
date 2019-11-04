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
        private abstract class BufferEnumeratorBase<T, TQueue> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmitting = 2;
            private const int _sCompleted = 3;
            private const int _sFinal = 4;

            private readonly ILinxObservable<T> _source;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly Observer _observer;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbDisposed = default;

            protected BufferEnumeratorBase(ILinxObservable<T> source, CancellationToken token)
            {
                _source = source;
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
                        Prune();
                        if (Queue.Count > 0)
                        {
                            Current = Dequeue();
                            if (Queue.Count == 0) // consumer now faster than producer
                                try { Queue.TrimExcess(); }
                                catch {/**/}
                            _state = _sEmitting;
                            _tsAccepting.SetResult(true);
                        }
                        else
                            _state = _sAccepting;
                        break;

                    case _sCompleted:
                        Prune();
                        if (Queue.Count > 0)
                        {
                            Current = Dequeue();
                            _state = _sCompleted;
                            _tsAccepting.SetResult(true);
                        }
                        else
                        {
                            Current = default;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            _tsAccepting.SetExceptionOrResult(_error, false);
                        }
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

            protected Queue<TQueue> Queue { get; } = new Queue<TQueue>();
            protected abstract void Enqueue(T item);
            protected abstract T Dequeue();
            protected abstract void Prune();

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
                        _state = _sCompleted;
                        Debug.Assert(Queue.Count == 0);
                        _ctr.Dispose();
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmitting:
                        _error = error;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        Queue.Clear();
                        break;

                    case _sCompleted when (Queue.Count > 0):
                        Debug.Assert(_error == null);
                        _error = error;
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
                private readonly BufferEnumeratorBase<T, TQueue> _e;
                public Observer(BufferEnumeratorBase<T, TQueue> enumerator) => _e = enumerator;

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
                                _e.Prune();
                                _e.Enqueue(value);
                                _e._state = _sEmitting;
                            }
                            catch (Exception ex)
                            {
                                _e._state = _sEmitting;
                                _e.OnError(ex);
                                return false;
                            }
                            return true;

                        default:
                            _e._state = state;
                            return false;
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
                            Debug.Assert(_e.Queue.Count == 0);
                            _e.Current = default;
                            _e._state = _sFinal;
                            _e._ctr.Dispose();
                            _e._atmbDisposed.SetResult();
                            _e._tsAccepting.SetResult(false);
                            break;

                        case _sEmitting:
                            _e._state = _sCompleted;
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