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
            return new BufferAsyncEnumerable<T>(source);
        }

        private sealed class BufferAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>, ILinxObservable<T>, ILinxObserver<T>
        {
            private const int _sEnumerator = 0;
            private const int _sInitial = 1;
            private const int _sAccepting = 2;
            private const int _sNext = 3;
            private const int _sLast = 4;

            private readonly ILinxObservable<T> _source;
            private CancellationTokenRegistration _ctr;
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private int _state;
            private Queue<T> _queue;
            private Exception _error;

            public BufferAsyncEnumerable(ILinxObservable<T> source) => _source = source;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sEnumerator)
                {
                    _state = state;
                    return new BufferAsyncEnumerable<T>(_source).GetAsyncEnumerator(token);
                }

                _state = _sInitial;
                Token = token;
                if (token.CanBeCanceled) _ctr = token.Register(() => Catch(new OperationCanceledException(token)));
                return this;
            }

            public override string ToString() => "Buffer";

            void ILinxObservable<T>.Subscribe(ILinxObserver<T> observer) => _source.Subscribe(observer);

            private void Catch(Exception error)
            {
                Debug.Assert(error != null);

                var state = Atomic.Lock(ref _state);

                if (_error != null)
                {
                    _state = state;
                    return;
                }

                switch (state)
                {
                    case _sEnumerator:
                        _state = _sEnumerator;
                        throw new InvalidOperationException();

                    case _sInitial:
                        _error = error;
                        _state = _sLast;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sAccepting:
                        _error = error;
                        _queue = null;
                        _state = _sNext;
                        _ctr.Dispose();
                        _tsMoveNext.SetException(error);
                        break;

                    case _sNext:
                        _error = error;
                        _queue = null;
                        _state = _sNext;
                        _ctr.Dispose();
                        break;

                    case _sLast:
                        if (_queue != null)
                        {
                            _error = error;
                            _queue = null;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                        }
                        _state = _sLast;
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
                    case _sEnumerator:
                    case _sInitial:
                        _state = _sEnumerator;
                        throw new InvalidOperationException();

                    case _sAccepting:
                        Debug.Assert(_error == null && (_queue == null || _queue.Count == 0));
                        Current = default;
                        _queue = null;
                        _state = _sLast;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsMoveNext.SetResult(false);
                        break;

                    case _sNext:
                        if (_queue == null || _queue.Count == 0)
                        {
                            _queue = null;
                            _state = _sLast;
                            _ctr.Dispose();
                            _atmbDisposed.SetResult();
                        }
                        else
                            _state = _sLast;
                        break;

                    case _sLast:
                        _state = _sLast;
                        break;

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
                    case _sEnumerator:
                        _state = _sEnumerator;
                        throw new InvalidOperationException();

                    case _sInitial:
                        _state = _sAccepting;
                        try { _source.Subscribe(this); }
                        catch (Exception ex) { ((ILinxObserver<T>)this).OnError(ex); }
                        break;

                    case _sNext:
                        if (_error != null)
                        {
                            Current = default;
                            _state = _sNext;
                            _tsMoveNext.SetException(_error);
                        }
                        else if (_queue == null || _queue.Count == 0)
                            _state = _sAccepting;
                        else
                        {
                            Current = _queue.Dequeue();
                            if (_queue.Count == 0) try { _queue.TrimExcess(); } catch { /**/ }
                            _state = _sNext;
                            _tsMoveNext.SetResult(true);
                        }
                        break;

                    case _sLast:
                        if (_queue == null || _queue.Count == 0)
                        {
                            _state = _sLast;
                            _tsMoveNext.SetExceptionOrResult(_error, false);
                        }
                        else
                        {
                            Debug.Assert(_error == null);
                            Current = _queue.Dequeue();
                            if (_queue.Count == 0)
                            {
                                _queue = null;
                                _state = _sLast;
                                _ctr.Dispose();
                                _atmbDisposed.SetResult();
                            }
                            else
                                _state = _sLast;
                            _tsMoveNext.SetResult(true);
                        }
                        break;

                    //case _sAccepting:
                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                return _tsMoveNext.Task;
            }

            async ValueTask IAsyncDisposable.DisposeAsync()
            {
                Catch(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                Current = default;
            }

            #endregion

            #region ILinxObserver<T> implementation

            public CancellationToken Token { get; private set; }

            bool ILinxObserver<T>.OnNext(T value)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerator:
                    case _sInitial:
                        _state = _sEnumerator;
                        throw new InvalidOperationException();

                    case _sAccepting:
                        Current = value;
                        _state = _sNext;
                        _tsMoveNext.SetResult(true);
                        return true;

                    case _sNext:
                        if (_error != null)
                        {
                            _state = _sNext;
                            return false;
                        }

                        try
                        {
                            if (_queue == null) _queue = new Queue<T>();
                            _queue.Enqueue(value);
                        }
                        catch (Exception ex)
                        {
                            _state = _sNext;
                            Catch(ex);
                            return false;
                        }
                        _state = _sNext;
                        return true;

                    case _sLast:
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
