﻿namespace Linx.Observable
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
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new LatestOneEnumerable<T>(source);
        }

        private sealed class LatestOneEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>, ILinxObservable<T>, ILinxObserver<T>
        {
            private const int _sEnumerable = 0;
            private const int _sInitial = 1;
            private const int _sAccepting = 2;
            private const int _sCurrentMutable = 3;
            private const int _sCurrentReadOnly = 4;
            private const int _sNext = 5;
            private const int _sLast = 6;
            private const int _sCompleted = 7;
            private const int _sFinal = 8;

            private readonly ILinxObservable<T> _source;
            private CancellationTokenRegistration _ctr;
            private readonly ManualResetValueTaskSource<bool> _tsMoveNext = new ManualResetValueTaskSource<bool>();
            private AsyncTaskMethodBuilder _atmbDisposed = new AsyncTaskMethodBuilder();
            private int _state;
            private T _current, _next;
            private Exception _error;

            public LatestOneEnumerable(ILinxObservable<T> source) => _source = source;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var state = Atomic.Lock(ref _state);
                if (state != _sEnumerable)
                {
                    _state = state;
                    return new LatestOneEnumerable<T>(_source).GetAsyncEnumerator(token);
                }

                _state = _sInitial;
                Token = token;
                if (token.CanBeCanceled) _ctr = token.Register(() => Catch(new OperationCanceledException(token)));
                return this;
            }

            public override string ToString() => "Latest";

            private void Catch(Exception error)
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
                        _current = _next = default;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        _tsMoveNext.SetException(error);
                        break;

                    case _sCurrentMutable:
                    case _sCurrentReadOnly:
                    case _sNext:
                        _error = error;
                        _next = default;
                        _state = _sCompleted;
                        _ctr.Dispose();
                        break;

                    case _sLast:
                        _error = error;
                        _next = default;
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
                    case _sEnumerable:
                    case _sInitial:
                        _state = state;
                        throw new InvalidOperationException();

                    case _sAccepting:
                        _current = _next = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsMoveNext.SetResult(false);
                        break;

                    case _sCurrentMutable:
                    case _sCurrentReadOnly:
                        _next = default;
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        break;

                    case _sNext:
                        _state = _sLast;
                        break;

                    case _sCompleted:
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    case _sLast:
                    case _sFinal:
                        _state = state;
                        break;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }
            }

            void ILinxObservable<T>.Subscribe(ILinxObserver<T> observer) => _source.Subscribe(observer); // NOP

            #region IAsyncEnumerator<T> implementation

            T IAsyncEnumerator<T>.Current
            {
                get
                {
                    var state = Atomic.Lock(ref _state);
                    var value = _current;
                    _state = state == _sCurrentMutable ? _sCurrentReadOnly : state;
                    return value;
                }
            }

            ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
            {
                _tsMoveNext.Reset();

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerable:
                        _state = _sEnumerable;
                        throw new InvalidOperationException();

                    case _sInitial:
                        _state = _sAccepting;
                        try { _source.Subscribe(this); }
                        catch (Exception ex) { ((ILinxObserver<T>)this).OnError(ex); }
                        break;

                    case _sCurrentMutable:
                    case _sCurrentReadOnly:
                        _state = _sAccepting;
                        break;

                    case _sNext:
                        _current = _next;
                        _state = _sCurrentMutable;
                        _tsMoveNext.SetResult(true);
                        break;

                    case _sLast:
                        _current = Linx.Clear(ref _next);
                        _state = _sFinal;
                        _ctr.Dispose();
                        _atmbDisposed.SetResult();
                        _tsMoveNext.SetResult(true);
                        break;

                    case _sCompleted:
                    case _sFinal:
                        _current = default;
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

            async ValueTask IAsyncDisposable.DisposeAsync()
            {
                Catch(AsyncEnumeratorDisposedException.Instance);
                await _atmbDisposed.Task.ConfigureAwait(false);
                _current = default;
            }

            #endregion

            #region ILinxObserver<T> implementation

            public CancellationToken Token { get; private set; }

            bool ILinxObserver<T>.OnNext(T value)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sEnumerable:
                    case _sInitial:
                        _state = _sEnumerable;
                        throw new InvalidOperationException();

                    case _sAccepting:
                        _current = value;
                        _state = _sCurrentMutable;
                        _tsMoveNext.SetResult(true);
                        return true;

                    case _sCurrentMutable:
                        _current = value;
                        _state = _sCurrentMutable;
                        return true;

                    case _sCurrentReadOnly:
                    case _sNext:
                        _next = value;
                        _state = state;
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
