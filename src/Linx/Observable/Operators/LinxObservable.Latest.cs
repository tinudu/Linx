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
        /// Ignores all but the latest element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LinxAsyncEnumerable.Create(token => new LatestOneEnumerator<T>(source, token));
        }

        /// <summary>
        /// Ignores all but the latest <paramref name="max"/> element.
        /// </summary>
        public static IAsyncEnumerable<T> Latest<T>(this ILinxObservable<T> source, int max)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));

            switch (max)
            {
                case 1:
                    return LinxAsyncEnumerable.Create(token => new LatestOneEnumerator<T>(source, token));
                case int.MaxValue:
                    return source.Buffer();
                default:
                    throw new NotImplementedException();
            }
        }

        private sealed class LatestOneEnumerator<T> : IAsyncEnumerator<T>
        {
            private const int _sInitial = 0;
            private const int _sAccepting = 1;
            private const int _sEmittingCurrentMutable = 2;
            private const int _sEmitting = 3;
            private const int _sEmittingNext = 4;
            private const int _sError = 5;
            private const int _sLast = 6;
            private const int _sFinal = 7;

            private readonly ILinxObservable<T> _source;
            private readonly CancellationToken _token;
            private readonly ManualResetValueTaskSource<bool> _tsAccepting = new ManualResetValueTaskSource<bool>();
            private readonly Observer _observer;
            private int _state;
            private T _current, _next;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbDisposed = default;

            public LatestOneEnumerator(ILinxObservable<T> source, CancellationToken token)
            {
                Debug.Assert(source != null);

                _source = source;
                _token = token;
                _observer = new Observer(this);
            }

            public T Current
            {
                get
                {
                    Atomic.CompareExchange(ref _state, _sEmitting, _sEmittingCurrentMutable);
                    return _current;
                }
            }

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

                    case _sEmittingCurrentMutable:
                    case _sEmitting:
                        _state = _sAccepting;
                        break;

                    case _sEmittingNext:
                        _current = _next;
                        _state = _sEmittingCurrentMutable;
                        _tsAccepting.SetResult(true);
                        break;

                    case _sError:
                        _current = default;
                        _state = _sError;
                        _tsAccepting.SetException(_error);
                        break;

                    case _sLast:
                        Debug.Assert(_error == null);
                        _current = Linx.Clear(ref _next);
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        _tsAccepting.SetResult(true);
                        break;

                    case _sFinal:
                        _current = default;
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
                        _current = default;
                        _error = error;
                        _state = _sError;
                        _tsAccepting.SetException(error);
                        break;

                    case _sEmittingCurrentMutable:
                    case _sEmitting:
                    case _sEmittingNext:
                        _next = default;
                        _error = error;
                        _state = _sError;
                        break;

                    case _sLast:
                        _next = default;
                        _error = error;
                        _state = _sFinal;
                        _atmbDisposed.SetResult();
                        break;

                    default: // Error, Final
                        _state = state;
                        break;
                }
            }

            private sealed class Observer : ILinxObserver<T>
            {
                private readonly LatestOneEnumerator<T> _e;
                public Observer(LatestOneEnumerator<T> enumerator) => _e = enumerator;

                public CancellationToken Token => _e._token;

                public bool OnNext(T value)
                {
                    var state = Atomic.Lock(ref _e._state);
                    switch (state)
                    {
                        case _sAccepting:
                            _e._current = value;
                            _e._state = _sEmittingCurrentMutable;
                            _e._tsAccepting.SetResult(true);
                            return true;

                        case _sEmittingCurrentMutable:
                            _e._current = value;
                            _e._state = _sEmittingCurrentMutable;
                            return true;

                        case _sEmitting:
                        case _sEmittingNext:
                            _e._next = value;
                            _e._state = _sEmittingNext;
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
                            _e._current = default;
                            _e._state = _sFinal;
                            _e._atmbDisposed.SetResult();
                            _e._tsAccepting.SetResult(false);
                            break;

                        case _sEmittingCurrentMutable:
                        case _sEmitting:
                            _e._state = _sFinal;
                            _e._atmbDisposed.SetResult();
                            break;

                        case _sEmittingNext:
                            _e._state = _sLast;
                            break;

                        case _sError:
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
