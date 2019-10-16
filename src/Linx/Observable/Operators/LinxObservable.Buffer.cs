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
            return source as BufferAsyncEnumerable<T> ?? new BufferAsyncEnumerable<T>(source);
        }

        private sealed class BufferAsyncEnumerable<T> : IAsyncEnumerable<T>, ILinxObservable<T>
        {
            private readonly ILinxObservable<T> _source;

            public BufferAsyncEnumerable(ILinxObservable<T> source) => _source = source;

            IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return new Enumerator(this, token);
            }

            void ILinxObservable<T>.Subscribe(ILinxObserver<T> observer) => _source.Subscribe(observer);

            public override string ToString() => "Buffer";

            private sealed class Enumerator : IAsyncEnumerator<T>, ILinxObserver<T>
            {
                private const int _sInitial = 0;
                private const int _sAccepting = 1; // !Q !E
                private const int _sEmitting = 2; // Q? !E
                private const int _sLast = 3; // Q !E
                private const int _sCompleted = 4; // !Q, E?
                private const int _sFinal = 5;

                private readonly ILinxObservable<T> _source;
                private readonly CancellationTokenSource _cts = new CancellationTokenSource();
                private readonly CancellationTokenRegistration _ctr;
                private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
                private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
                private int _state;
                private Queue<T> _queue;
                private Exception _error;

                public Enumerator(ILinxObservable<T> sourc, CancellationToken token)
                {
                    _source = sourc;
                    if (token.CanBeCanceled) _ctr = token.Register(() => Catch(new OperationCanceledException(token)));
                }

                private void Catch(Exception error)
                {
                    Debug.Assert(error != null);

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _error = error;
                            _state = _sFinal;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            _atmbDisposed.SetResult();
                            break;

                        case _sAccepting:
                            Current = default;
                            _error = error;
                            _queue = null;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            _tsMoveNext.SetException(error);
                            break;

                        case _sEmitting:
                            _error = error;
                            _queue = null;
                            _state = _sCompleted;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            break;

                        case _sLast:
                            _error = error;
                            _queue = null;
                            _state = _sFinal;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                            break;

                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            break;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                private void Finally()
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sInitial;
                            throw new InvalidOperationException();

                        case _sAccepting:
                            Current = default;
                            _queue = null;
                            _state = _sFinal;
                            _ctr.Dispose();
                            try { _cts.Cancel(); } catch { /**/ }
                            _atmbDisposed.SetResult();
                            _tsMoveNext.SetResult(false);
                            break;

                        case _sEmitting:
                            if (_queue == null || _queue.Count == 0)
                            {
                                _queue = null;
                                _state = _sFinal;
                                _ctr.Dispose();
                                try { _cts.Cancel(); } catch { /**/ }
                                _atmbDisposed.SetResult();
                            }
                            else
                            {
                                _state = _sLast;
                                try { _cts.Cancel(); } catch { /**/ }
                            }

                            break;

                        case _sCompleted:
                            _state = _sFinal;
                            _atmbDisposed.SetResult();
                            break;

                        case _sFinal:
                            _state = state;
                            break;

                        //case _sLast:
                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                #region IAsyncEnumerator<T> implementation

                public T Current { get; private set; }

                ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
                {
                    _tsMoveNext.Reset();

                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sAccepting;
                            try { _source.Subscribe(this); }
                            catch (Exception ex) { ((ILinxObserver<T>)this).OnError(ex); }
                            break;

                        case _sEmitting:
                            if (_queue == null || _queue.Count == 0)
                                _state = _sAccepting;
                            else
                            {
                                Current = _queue.Dequeue();
                                if (_queue.Count == 0) try { _queue.TrimExcess(); } catch { /**/ }
                                _state = _sEmitting;
                                _tsMoveNext.SetResult(true);
                            }
                            break;

                        case _sLast:
                            Debug.Assert(_error == null && _queue.Count > 0);
                            Current = _queue.Dequeue();
                            if (_queue.Count > 0)
                                _state = _sLast;
                            else
                            {
                                _queue = null;
                                _state = _sFinal;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            _tsMoveNext.SetResult(true);
                            break;

                        case _sCompleted:
                        case _sFinal:
                            Current = default;
                            _state = state;
                            _tsMoveNext.SetExceptionOrResult(_error, false);
                            break;

                        //case _sAccepting:
                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }

                    return _tsMoveNext.Task;
                }

                ValueTask IAsyncDisposable.DisposeAsync()
                {
                    Catch(AsyncEnumeratorDisposedException.Instance);
                    return new ValueTask(_atmbDisposed.Task);
                }

                #endregion

                #region ILinxObserver<T> implementation

                CancellationToken ILinxObserver<T>.Token => _cts.Token;

                bool ILinxObserver<T>.OnNext(T value)
                {
                    var state = Atomic.Lock(ref _state);
                    switch (state)
                    {
                        case _sInitial:
                            _state = _sInitial;
                            throw new InvalidOperationException();

                        case _sAccepting:
                            Current = value;
                            _state = _sEmitting;
                            _tsMoveNext.SetResult(true);
                            return true;

                        case _sEmitting:
                            try
                            {
                                if (_queue == null) _queue = new Queue<T>();
                                _queue.Enqueue(value);
                            }
                            catch (Exception ex)
                            {
                                _state = _sEmitting;
                                Catch(ex);
                                return false;
                            }
                            _state = _sEmitting;
                            return true;

                        case _sLast:
                        case _sCompleted:
                        case _sFinal:
                            _state = state;
                            return false;

                        default:
                            _state = state;
                            throw new Exception(state + "???");
                    }
                }

                void ILinxObserver<T>.OnError(Exception error)
                {
                    Catch(error ?? new ArgumentNullException(nameof(error)));
                    Finally();
                }

                void ILinxObserver<T>.OnCompleted() => Finally();

                #endregion
            }
        }
    }
}
