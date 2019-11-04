namespace Linx.Observable
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    partial class LinxObservable
    {
        private sealed class AnonymousLinxObserver<T> : ILinxObserver<T>
        {
            private interface IState
            {
                bool OnNext(T value);
                void OnError(Exception error);
                void OnCompleted();
            }

            private readonly Action<Exception> _onError;
            private readonly Action _onCompleted;
            private IState _state;

            public AnonymousLinxObserver(Func<T, bool> onNext, Action<Exception> onError, Action onCompleted, CancellationToken token)
            {
                Debug.Assert(onNext != null);
                Debug.Assert(onError != null);
                Debug.Assert(onCompleted != null);

                _onError = onError;
                _onCompleted = onCompleted;
                Token = token;
                _state = new InitialState(this, onNext);
            }

            public CancellationToken Token { get; }
            public bool OnNext(T value) => _state.OnNext(value);
            public void OnError(Exception error) => _state.OnError(error ?? new ArgumentNullException(nameof(error)));
            public void OnCompleted() => _state.OnCompleted();

            private sealed class InitialState : IState
            {
                private readonly AnonymousLinxObserver<T> _parent;
                private readonly Func<T, bool> _onNext;

                public InitialState(AnonymousLinxObserver<T> parent, Func<T, bool> onNext)
                {
                    _parent = parent;
                    _onNext = onNext;
                }

                public bool OnNext(T value)
                {
                    try
                    {
                        if (_onNext(value)) return true;
                        Interlocked.CompareExchange(ref _parent._state, new CompletedState(_parent), this);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.CompareExchange(ref _parent._state, new ErrorState(_parent, ex), this);
                    }

                    return false;
                }

                public void OnError(Exception error)
                {
                    if (Interlocked.CompareExchange(ref _parent._state, FinalState.Instance, this) != this) return;
                    _parent._onError(error);
                }

                public void OnCompleted()
                {
                    if (Interlocked.CompareExchange(ref _parent._state, FinalState.Instance, this) != this) return;
                    _parent._onCompleted();
                }
            }

            private sealed class CompletedState : IState
            {
                private readonly AnonymousLinxObserver<T> _parent;

                public CompletedState(AnonymousLinxObserver<T> parent) => _parent = parent;

                public bool OnNext(T _) => false;
                public void OnError(Exception _) => OnCompleted();

                public void OnCompleted()
                {
                    if (Interlocked.CompareExchange(ref _parent._state, FinalState.Instance, this) != this) return;
                    _parent._onCompleted();
                }
            }

            private sealed class ErrorState : IState
            {
                private readonly AnonymousLinxObserver<T> _parent;
                private readonly Exception _error;

                public ErrorState(AnonymousLinxObserver<T> parent, Exception error)
                {
                    _parent = parent;
                    _error = error;
                }

                public bool OnNext(T _) => false;
                public void OnError(Exception _) => OnCompleted();

                public void OnCompleted()
                {
                    if (Interlocked.CompareExchange(ref _parent._state, FinalState.Instance, this) != this) return;
                    _parent._onError(_error);
                }
            }

            private sealed class FinalState : IState
            {
                public static FinalState Instance { get; } = new FinalState();
                private FinalState() { }

                public bool OnNext(T _) => false;

                public void OnError(Exception error) { }

                public void OnCompleted() { }
            }
        }
    }
}