namespace Linx.Reactive
{
    using System;
    using System.Threading;

    /// <summary>
    /// Anonymous <see cref="IAsyncEnumerableObs{T}"/> implementation.
    /// </summary>
    public sealed class AnonymousAsyncEnumerable<T> : IAsyncEnumerableObs<T>
    {
        private readonly Func<CancellationToken, IAsyncEnumeratorObs<T>> _getEnumerator;

        /// <summary>
        /// Initialize with a <see cref="IAsyncEnumerableObs{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
        /// </summary>
        /// <param name="getEnumerator"></param>
        public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumeratorObs<T>> getEnumerator) => _getEnumerator = getEnumerator ?? throw new ArgumentNullException(nameof(getEnumerator));

        /// <inheritdoc />
        public IAsyncEnumeratorObs<T> GetAsyncEnumerator(CancellationToken token) => _getEnumerator(token);
    }
}
