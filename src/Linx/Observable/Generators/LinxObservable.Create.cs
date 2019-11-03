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
        {
            if (subscribe == null) throw new ArgumentNullException(nameof(subscribe));
            return new AnonymousLinxObservable<T>(subscribe, name);
        }
    }
}
