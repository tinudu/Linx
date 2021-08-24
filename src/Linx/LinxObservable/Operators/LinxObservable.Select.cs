using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.LinxObservable
{
    partial class LinxObservable
    {
        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static ILinxObservable<R> Select<S, R>(this ILinxObservable<S> source, Func<S, R> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return LinxObservable.Create<R>(observer => source.Subscribe(new SelectObserver1<S, R>(selector, observer)));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static ILinxObservable<R> Select<S, R>(this ILinxObservable<S> source, Func<S, int, R> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return LinxObservable.Create<R>(observer => source.Subscribe(new SelectObserver2<S, R>(selector, observer)));
        }

        private sealed class SelectObserver1<S, R> : ILinxObserver<S>
        {
            private const int _sInitial = 0;
            private const int _sError = 1;
            private const int _sFinal = 2;

            private readonly Func<S, R> _selector;
            private readonly ILinxObserver<R> _observer;
            private readonly CancellationTokenSource _cts = new();
            private readonly CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;

            public SelectObserver1(Func<S, R> selector, ILinxObserver<R> observer)
            {
                _selector = selector;
                _observer = observer;

                var token = observer.Token;
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetError(new OperationCanceledException(token)));
            }

            public CancellationToken Token => _cts.Token;

            public void OnNext(S item)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sInitial;
                        try { _observer.OnNext(_selector(item)); }
                        catch (Exception error) { SetError(error); }
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            public void OnError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sFinal;
                        _ctr.Dispose();
                        _observer.OnError(error);
                        _cts.Cancel();
                        break;

                    case _sError:
                        error = Linx.Clear(ref _error);
                        _state = _sFinal;
                        _observer.OnError(error);
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            public void OnCompleted()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sFinal;
                        _ctr.Dispose();
                        _observer.OnCompleted();
                        _cts.Cancel();
                        break;

                    case _sError:
                        var error = Linx.Clear(ref _error);
                        _state = _sFinal;
                        _observer.OnError(error);
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            private void SetError(Exception error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _cts.Cancel();
                        break;

                    default:
                        _state = state;
                        break;
                }
            }
        }

        private sealed class SelectObserver2<S, R> : ILinxObserver<S>
        {
            private const int _sInitial = 0;
            private const int _sError = 1;
            private const int _sFinal = 2;

            private readonly Func<S, int, R> _selector;
            private readonly ILinxObserver<R> _observer;
            private readonly CancellationTokenSource _cts = new();
            private readonly CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;
            private int _index;

            public SelectObserver2(Func<S, int, R> selector, ILinxObserver<R> observer)
            {
                _selector = selector;
                _observer = observer;

                var token = observer.Token;
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetError(new OperationCanceledException(token)));
            }

            public CancellationToken Token => _cts.Token;

            public void OnNext(S item)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        var index = unchecked(_index++);
                        _state = _sInitial;
                        try { _observer.OnNext(_selector(item, index)); }
                        catch (Exception error) { SetError(error); }
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            public void OnError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));

                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sFinal;
                        _ctr.Dispose();
                        _observer.OnError(error);
                        _cts.Cancel();
                        break;

                    case _sError:
                        error = Linx.Clear(ref _error);
                        _state = _sFinal;
                        _observer.OnError(error);
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            public void OnCompleted()
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _state = _sFinal;
                        _ctr.Dispose();
                        _observer.OnCompleted();
                        _cts.Cancel();
                        break;

                    case _sError:
                        var error = Linx.Clear(ref _error);
                        _state = _sFinal;
                        _observer.OnError(error);
                        break;

                    default:
                        _state = state;
                        break;
                }
            }

            private void SetError(Exception error)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sInitial:
                        _error = error;
                        _state = _sError;
                        _ctr.Dispose();
                        _cts.Cancel();
                        break;

                    default:
                        _state = state;
                        break;
                }
            }
        }
    }
}
