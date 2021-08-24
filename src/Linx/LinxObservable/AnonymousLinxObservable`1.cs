using System;

namespace Linx.LinxObservable
{
    internal sealed class AnonymousLinxObservable<T> : ILinxObservable<T>
    {
        private readonly Action<ILinxObserver<T>> _subscribe;

        public AnonymousLinxObservable(Action<ILinxObserver<T>> subscribe)
        {
            if (subscribe is null) throw new ArgumentNullException(nameof(subscribe));
            _subscribe = subscribe;
        }

        public void Subscribe(ILinxObserver<T> observer)
        {
            if (observer is null) throw new ArgumentNullException(nameof(observer));

            try
            {
                observer.Token.ThrowIfCancellationRequested();
                _subscribe(observer);
            }
            catch (Exception error)
            {
                observer.OnError(error);
            }
        }
    }
}