namespace Linx.Reactive
{
    using System.Threading.Tasks;
    using Coroutines;

    /// <summary>
    /// Enumerator where elements are retrieved asynchronously.
    /// </summary>
    public interface IAsyncEnumerator<out T>
    {
        /// <summary>
        /// Get the next element.
        /// </summary>
        ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext = false);

        /// <summary>
        /// Gets the current item after <see cref="MoveNextAsync"/> returned true.
        /// </summary>
        T Current { get; }

        /// <summary>
        /// Asynchronously dispose of any resources.
        /// </summary>
        Task DisposeAsync();
    }
}
