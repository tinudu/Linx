namespace Linx.Coroutines
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Factory for completed <see cref="ICoAwaiter"/> instances.
    /// </summary>
    public static class CoAwaiter
    {
        /// <summary>
        /// Gets a <see cref="ICoAwaiter"/> singleton that is completed.
        /// </summary>
        public static ICoAwaiter Completed => CompletedCoAwaiter<Unit>.Default;

        /// <summary>
        /// Gets a <see cref="ICoAwaiter{T}"/> singleton that is completed with the default value.
        /// </summary>
        public static ICoAwaiter<T> Default<T>() => CompletedCoAwaiter<T>.Default;

        /// <summary>
        /// Gets a <see cref="ICoAwaiter{T}"/> singleton that is completed with true.
        /// </summary>
        public static ICoAwaiter<bool> True { get; } = new CompletedCoAwaiter<bool>(true);

        /// <summary>
        /// Gets a <see cref="ICoAwaiter{T}"/> singleton that is completed with false.
        /// </summary>
        public static ICoAwaiter<bool> False => CompletedCoAwaiter<bool>.Default;

        /// <summary>
        /// Gets a <see cref="ICoAwaiter{T}"/> instance that is completed with the specified result.
        /// </summary>
        public static ICoAwaiter<T> FromResult<T>(T result) => new CompletedCoAwaiter<T>(result);

        /// <summary>
        /// Gets a <see cref="ICoAwaiter"/> instance that is completed with the specified exception.
        /// </summary>
        public static ICoAwaiter FromException(Exception exception) => new CompletedCoAwaiter<Unit>(exception);

        /// <summary>
        /// Gets a <see cref="ICoAwaiter{T}"/> instance that is completed with the specified exception.
        /// </summary>
        public static ICoAwaiter<T> FromException<T>(Exception exception) => new CompletedCoAwaiter<T>(exception);

        [DebuggerStepThrough]
        private sealed class CompletedCoAwaiter<T> : ICoAwaiter<T>
        {
            public static CompletedCoAwaiter<T> Default { get; } = new CompletedCoAwaiter<T>(default(T));

            private readonly Func<T> _getResult;

            public CompletedCoAwaiter(T result) => _getResult = () => result;

            public CompletedCoAwaiter(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));
                _getResult = () => throw exception;
            }

            public bool IsCompleted => true;

            public void OnCompleted(Action continuation) => continuation();

            void ICoAwaiter.GetResult() => _getResult();
            T ICoAwaiter<T>.GetResult() => _getResult();

            ICoAwaiter ICoAwaiter.GetAwaiter() => this;
            public ICoAwaiter<T> GetAwaiter() => this;
        }
    }
}
