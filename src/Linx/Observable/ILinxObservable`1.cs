namespace Linx.Observable
{
    /// <summary>
    /// An object that produces a sequence.
    /// </summary>
    /// <remarks>
    /// Other than in the <see cref="System.IObservable{T}"/> world,
    /// a <see cref="ILinxObservable{T}"/> is required to notify completion
    /// if <see cref="ILinxObserver{T}.OnNext"/> returns false or throws an exception.
    /// </remarks>
    public interface ILinxObservable<out T>
    {
        /// <summary>
        /// Start producing a sequence.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"><paramref name="observer"/> is null.</exception>
        /// <remarks>Implementations are required to catch exceptions and notify the <paramref name="observer"/>.</remarks>
        void Subscribe(ILinxObserver<T> observer);
    }
}
