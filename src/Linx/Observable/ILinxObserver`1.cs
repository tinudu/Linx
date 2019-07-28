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
        /// Accepts the next sequence element.
        /// </summary>
        /// <returns>Whether more elements are requested.</returns>
        /// <exception cref="OperationCanceledException">The <see cref="Token"/> requested cancellation.</exception>
        bool OnNext(T value);

        /// <summary>
        /// Notify completion with error.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="error"/> is null.</exception>
        void OnError(Exception error);

        /// <summary>
        /// Notify successful completion.
        /// </summary>
        void OnCompleted();
    }
}
