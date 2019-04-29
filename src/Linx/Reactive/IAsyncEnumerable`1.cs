namespace Linx.Reactive
{
    using System.Threading;

    /// <summary>
    /// Asynchronous enumerable.
    /// </summary>
    public interface IAsyncEnumerableObs<out T>
    {
        /// <summary>
        /// Get an async enumerator.
        /// </summary>
        IAsyncEnumeratorObs<T> GetAsyncEnumerator(CancellationToken token);
    }
}
