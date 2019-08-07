namespace Linx.Observable
{
    /// <summary>
    /// An object that produces a sequence.
    /// </summary>
    /// <remarks>
    /// Other than in the <see cref="System.IObservable{T}"/> world,
    /// a <see cref="ILinxObservable{T}"/> is considered subscribed to
    /// as long as it hasn't notified completion to the <see cref="ILinxObserver{T}"/>.
    /// </remarks>
    public interface ILinxObservable<out T>
    {
        /// <summary>
        /// Start producing a sequence.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"><paramref name="observer"/> is null.</exception>
        void Subscribe(ILinxObserver<T> observer);
    }
}
