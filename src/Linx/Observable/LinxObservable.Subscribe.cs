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
            token.ThrowIfCancellationRequested();
            source.Subscribe(new AnonymousLinxObserver<T>(onNext, onError, onCompleted, token));
        }

        private sealed class AnonymousLinxObserver<T> : ILinxObserver<T>
        {
            private readonly Func<T, bool> _onNext;
            private readonly Action<Exception> _onError;
            private readonly Action _onCompleted;

            public AnonymousLinxObserver(Func<T, bool> onNext, Action<Exception> onError, Action onCompleted, CancellationToken token)
            {
                _onNext = onNext;
                _onError = onError;
                _onCompleted = onCompleted;
                Token = token;
            }

            public CancellationToken Token { get; }
            public bool OnNext(T value) => _onNext(value);
            public void OnError(Exception error) => _onError(error);
            public void OnCompleted() => _onCompleted();
        }
    }
}
