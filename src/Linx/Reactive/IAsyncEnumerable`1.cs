namespace Linx.Reactive
{
    using System.Threading;

    /// <summary>
    /// Asynchronous enumerable.
    /// </summary>
    public interface IAsyncEnumerable<out T>
    {
        /// <summary>
        /// Get an async enumerator.
        /// </summary>
        IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token);
    }
}
