namespace Linx.Observable
{
    using System;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Subscribe an observer create from the specified delegates and token to the specified source.
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

            source.Subscribe(new AnonymousLinxObserver<T>(onNext, onError, onCompleted, token));
        }

        private sealed class AnonymousLinxObserver<T> : ILinxObserver<T>
        {
            private Delegates _delegates;

            public AnonymousLinxObserver(Func<T, bool> onNext, Action<Exception> onError, Action onCompleted, CancellationToken token)
            {
                _delegates = new Delegates(onNext, onError, onCompleted);
                Token = token;
            }

            public CancellationToken Token { get; }

            public bool OnNext(T value)
            {
                var delegates = _delegates;
                if (delegates == null) return false;
                Token.ThrowIfCancellationRequested();
                return delegates.OnNext(value);
            }

            public void OnError(Exception error)
            {
                if (error == null) throw new ArgumentNullException(nameof(error));
                Interlocked.Exchange(ref _delegates, null).OnError(error);
            }

            public void OnCompleted()
            {
                Interlocked.Exchange(ref _delegates, null)?.OnCompleted();
            }

            private sealed class Delegates
            {
                public readonly Func<T, bool> OnNext;
                public readonly Action<Exception> OnError;
                public readonly Action OnCompleted;

                public Delegates(Func<T, bool> onNext, Action<Exception> onError, Action onCompleted)
                {
                    OnNext = onNext;
                    OnError = onError;
                    OnCompleted = onCompleted;
                }
            }

        }
    }
}
