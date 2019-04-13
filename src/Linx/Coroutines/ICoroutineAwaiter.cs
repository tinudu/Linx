namespace Linx.Coroutines
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Asynchronous result of a coroutine call.
    /// </summary>
    public interface ICoroutineAwaiter : INotifyCompletion
    {
        /// <summary>
        /// Gets whether the result is awailable.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets the result or throws an exception.
        /// </summary>
        void GetResult();

        /// <summary>
        /// Supports async/await syntax.
        /// </summary>
        /// <returns>This instance.</returns>
        ICoroutineAwaiter GetAwaiter();
    }

    /// <summary>
    /// Generic <see cref="ICoroutineAwaiter"/>.
    /// </summary>
    public interface ICoroutineAwaiter<out T> : ICoroutineAwaiter
    {
        /// <summary>
        /// Gets the result or throws an exception.
        /// </summary>
        new T GetResult();

        /// <summary>
        /// Supports async/await syntax.
        /// </summary>
        /// <returns>This instance.</returns>
        new ICoroutineAwaiter<T> GetAwaiter();
    }
}
