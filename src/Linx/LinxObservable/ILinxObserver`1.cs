using System;
using System.Threading;

namespace Linx.LinxObservable
{
    /// <summary>
    /// Provides a mechanism for receiving push-based notifications.
    /// </summary>
    public interface ILinxObserver<in T>
    {
        /// <summary>
        /// <see cref="CancellationToken"/> on which the observer requests to be unsubscribed.
        /// </summary>
        /// <remarks>The source to which the observer is subscribed should clean up its resources, then signal a <see cref="OperationCanceledException"/>.</remarks>
        CancellationToken Token { get; }

        /// <summary>
        /// Provides the observer with new data.
        /// </summary>
        /// <param name="item">The current notification information.</param>
        void OnNext(T item);

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// </summary>
        void OnCompleted();

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        void OnError(Exception error);
    }
}
