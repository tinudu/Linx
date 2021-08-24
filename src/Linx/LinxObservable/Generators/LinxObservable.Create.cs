using System;

namespace Linx.LinxObservable
{
    partial class LinxObservable
    {
        /// <summary>
        /// Create a <see cref="ILinxObservable{T}"/> by specifying a delegate of the subscribe method.
        /// </summary>
        /// <remarks>
        /// The implementation performs a null check on the observer and handles the case when its token requests cancellation on subscription.
        /// </remarks>
        public static ILinxObservable<T> Create<T>(Action<ILinxObserver<T>> subscribe) => new AnonymousLinxObservable<T>(subscribe);
    }
}
