namespace Linx.Coroutines
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Asynchronous result of a coroutine call.
    /// </summary>
    /// <remarks>Can be awaited only once to enable recycling.</remarks>
    public interface ICoAwaiter : INotifyCompletion
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
        ICoAwaiter GetAwaiter();
    }

    /// <summary>
    /// Generic <see cref="ICoAwaiter"/>.
    /// </summary>
    public interface ICoAwaiter<out T> : ICoAwaiter
    {
        /// <summary>
        /// Gets the result or throws an exception.
        /// </summary>
        new T GetResult();

        /// <summary>
        /// Supports async/await syntax.
        /// </summary>
        /// <returns>This instance.</returns>
        new ICoAwaiter<T> GetAwaiter();
    }
}
