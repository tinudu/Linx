namespace Linx.Observable
{
    using System;
    using System.Diagnostics;

    internal sealed class AnonymousLinxObservable<T> : ILinxObservable<T>
    {
        private readonly Action<ILinxObserver<T>> _subscribe;
        private readonly string _name;

        public AnonymousLinxObservable(Action<ILinxObserver<T>> subscribe, string name)
        {
            Debug.Assert(subscribe != null);
            _subscribe = subscribe;
            _name = name ?? nameof(AnonymousLinxObservable<T>);
        }

        public void Subscribe(ILinxObserver<T> observer) => _subscribe(observer);

        public override string ToString() => _name;

        public AnonymousLinxObservable<T> WithName(string name) => name == _name ? this : new AnonymousLinxObservable<T>(_subscribe, name);
    }
}