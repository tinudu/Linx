namespace Linx.Observable
{
    using System;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Subscribe an observer created from the specified delegates and token to the specified source.
        /// </summary>
        public static void Subscribe<T>(
            this ILinxObservable<T> source,
            Func<T, bool> onNext,
            Action<Exception> onError,
            Action onCompleted,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            if (onError == null) throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null) throw new ArgumentNullException(nameof(onCompleted));

            AnonymousLinxObserver<T> observer;
            try
            {
                token.ThrowIfCancellationRequested();
                observer = new AnonymousLinxObserver<T>(onNext, onError, onCompleted, token);
            }
            catch (Exception ex) { onError(ex); return; }

            try { source.Subscribe(observer); }
            catch (Exception ex) { observer.OnError(ex); }
        }

        private sealed class AnonymousLinxObserver<T> : ILinxObserver<T>
        {
            private const int _sSubscribed = 0;
            private const int _sCompleted = 1;
            private const int _sFinal = 2;

            private Func<T, bool> _onNext;
            private Action<Exception> _onError;
            private Action _onCompleted;
            private CancellationTokenRegistration _ctr;
            private int _state;
            private Exception _error;

            public AnonymousLinxObserver(Func<T, bool> onNext, Action<Exception> onError, Action onCompleted, CancellationToken token)
            {
                _onNext = onNext;
                _onError = onError;
                _onCompleted = onCompleted;
                Token = token;
                if (token.CanBeCanceled) _ctr = token.Register(() => SetCompleted(new OperationCanceledException(token)));
            }

            public CancellationToken Token { get; }

            public bool OnNext(T value)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sSubscribed)
                {
                    _state = state;
                    return false;
                }

                var onNext = _onNext;
                _state = _sSubscribed;

                Exception error;
                try
                {
                    if (onNext(value)) return true;
                    error = null;
                }
                catch (Exception ex) { error = ex; }
                SetCompleted(error);
                return false;
            }

            public void OnError(Exception error)
            {
                SetCompleted(error ?? new ArgumentNullException(nameof(error)));
                SetFinal();
            }

            public void OnCompleted() => SetFinal();

            private void SetCompleted(Exception errorOpt)
            {
                var state = Atomic.Lock(ref _state);
                switch (state)
                {
                    case _sSubscribed:
                        _error = errorOpt;
                        _onNext = null;
                        _state = _sCompleted;
                        _ctr.Dispose();
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

            private void SetFinal()
            {
                var state = Atomic.Lock(ref _state);
                Action<Exception> onError;
                Action onCompleted;
                switch (state)
                {
                    case _sSubscribed:
                        _onNext = null;
                        onError = Linx.Clear(ref _onError);
                        onCompleted = Linx.Clear(ref _onCompleted);
                        _state = _sFinal;
                        _ctr.Dispose();
                        break;

                    case _sCompleted:
                        onError = Linx.Clear(ref _onError);
                        onCompleted = Linx.Clear(ref _onCompleted);
                        _state = _sFinal;
                        break;

                    case _sFinal:
                        _state = _sFinal;
                        return;

                    default:
                        _state = state;
                        throw new Exception(state + "???");
                }

                if (_error == null) onCompleted();
                else onError(_error);
            }
        }
    }
}
