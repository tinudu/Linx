namespace Linx.Coroutines
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Factory for completed <see cref="ICoroutineAwaiter"/> instances.
    /// </summary>
    public static class CoroutineAwaiter
    {
        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter"/> singleton that is completed.
        /// </summary>
        public static ICoroutineAwaiter Completed => CompletedCoroutineAwaiter<Unit>.Default;

        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter{T}"/> singleton that is completed with the default value.
        /// </summary>
        public static ICoroutineAwaiter<T> Default<T>() => CompletedCoroutineAwaiter<T>.Default;

        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter{T}"/> singleton that is completed with true.
        /// </summary>
        public static ICoroutineAwaiter<bool> True { get; } = new CompletedCoroutineAwaiter<bool>(true);

        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter{T}"/> singleton that is completed with false.
        /// </summary>
        public static ICoroutineAwaiter<bool> False => CompletedCoroutineAwaiter<bool>.Default;

        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter{T}"/> instance that is completed with the specified result.
        /// </summary>
        public static ICoroutineAwaiter<T> FromResult<T>(T result) => new CompletedCoroutineAwaiter<T>(result);

        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter"/> instance that is completed with the specified exception.
        /// </summary>
        public static ICoroutineAwaiter FromException(Exception exception) => new CompletedCoroutineAwaiter<Unit>(exception);

        /// <summary>
        /// Gets a <see cref="ICoroutineAwaiter{T}"/> instance that is completed with the specified exception.
        /// </summary>
        public static ICoroutineAwaiter<T> FromException<T>(Exception exception) => new CompletedCoroutineAwaiter<T>(exception);

        [DebuggerStepThrough]
        private sealed class CompletedCoroutineAwaiter<T> : ICoroutineAwaiter<T>
        {
            public static CompletedCoroutineAwaiter<T> Default { get; } = new CompletedCoroutineAwaiter<T>(default(T));

            private readonly Func<T> _getResult;

            public CompletedCoroutineAwaiter(T result) => _getResult = () => result;

            public CompletedCoroutineAwaiter(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));
                _getResult = () => throw exception;
            }

            public bool IsCompleted => true;

            public void OnCompleted(Action continuation) => continuation();

            void ICoroutineAwaiter.GetResult() => _getResult();
            T ICoroutineAwaiter<T>.GetResult() => _getResult();

            ICoroutineAwaiter ICoroutineAwaiter.GetAwaiter() => this;
            public ICoroutineAwaiter<T> GetAwaiter() => this;
        }
    }
}
