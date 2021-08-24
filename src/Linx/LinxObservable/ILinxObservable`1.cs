namespace Linx.LinxObservable
{
    /// <summary>
    /// Defines a provider for push-based notification.
    /// </summary>
    public interface ILinxObservable<out T>
    {
        /// <summary>
        /// Notifies the provider that an observer is to receive notifications.
        /// </summary>
        /// <param name="observer">The object that is to receive notifications.</param>
        void Subscribe(ILinxObserver<T> observer);
    }
}
