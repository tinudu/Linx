namespace Linx.AsyncEnumerable
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
        private readonly string _name;

        /// <summary>
        /// Initialize with a <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
        /// </summary>
        public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator, string name)
        {
            _getEnumerator = getEnumerator ?? throw new ArgumentNullException(nameof(getEnumerator));
            _name = name ?? nameof(AnonymousAsyncEnumerable<T>);
        }

        /// <inheritdoc />
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return _getEnumerator(token);
        }

        /// <inheritdoc />
        public override string ToString() => _name;
    }
}
