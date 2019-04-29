namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Anonymous <see cref="IAsyncEnumerable{T}"/> implementation.
    /// </summary>
    public sealed class AnonymousAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Func<CancellationToken, IAsyncEnumerator<T>> _getEnumerator;

        /// <summary>
        /// Initialize with a <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
        /// </summary>
        /// <param name="getEnumerator"></param>
        public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator) => _getEnumerator = getEnumerator ?? throw new ArgumentNullException(nameof(getEnumerator));

        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => _getEnumerator(token);
    }
}
