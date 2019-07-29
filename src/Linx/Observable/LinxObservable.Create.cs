namespace Linx.Observable
{
    using System;

    partial class LinxObservable
    {
        /// <summary>
        /// Create a anonymous <see cref="ILinxObservable{T}"/> from the specified subscribe action.
        /// </summary>
        /// <param name="subscribe">Implementation of <see cref="ILinxObservable{T}.Subscribe"/>.</param>
        public static ILinxObservable<T> Create<T>(Action<ILinxObserver<T>> subscribe) => new AnonymousLinxObservable<T>(subscribe);

        private sealed class AnonymousLinxObservable<T> : ILinxObservable<T>
        {
            private readonly Action<ILinxObserver<T>> _subscribe;

            public AnonymousLinxObservable(Action<ILinxObserver<T>> subscribe) => _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));

            public void Subscribe(ILinxObserver<T> observer)
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                try
                {
                    observer.Token.ThrowIfCancellationRequested();
                    _subscribe(observer);
                }
                catch (Exception ex) { observer.OnError(ex); }
            }
        }
    }
}
