namespace Linx.Observable
{
    using System;
    using System.Threading;

    /// <summary>
    /// An object that accepts notifications.
    /// </summary>
    public interface ILinxObserver<in T>
    {
        /// <summary>
        /// Token on which the observer requests cancellation.
        /// </summary>
        CancellationToken Token { get; }

        /// <summary>
        /// Notify the next sequence element.
        /// </summary>
        /// <returns>Whether more elements are requested.</returns>
        bool OnNext(T value);

        /// <summary>
        /// Notify completion with error.
        /// </summary>
        void OnError(Exception error);

        /// <summary>
        /// Notify successful completion.
        /// </summary>
        void OnCompleted();
    }
}
