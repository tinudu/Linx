namespace Linx.Observable
{
    using System;
    using System.Runtime.CompilerServices;

    partial class LinxObservable
    {
        /// <summary>
        /// Create an anonymous <see cref="ILinxObservable{T}"/> from the specified subscribe action.
        /// </summary>
        public static ILinxObservable<T> Create<T>(
            Action<ILinxObserver<T>> subscribe, 
            [CallerMemberName]string name = default) 
            => new AnonymousLinxObservable<T>(subscribe, name);

        private sealed class AnonymousLinxObservable<T> : ILinxObservable<T>
        {
            private readonly Action<ILinxObserver<T>> _subscribe;
            private readonly string _name;

            public AnonymousLinxObservable(Action<ILinxObserver<T>> subscribe, string name)
            {
                _subscribe = subscribe ?? throw new ArgumentNullException(nameof(subscribe));
                _name = name ?? nameof(AnonymousLinxObservable<T>);
            }

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

            public override string ToString() => _name;
        }
    }
}
