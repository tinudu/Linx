namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>
    /// Anonymous <see cref="IAsyncEnumerable{T}"/> implementation.
    /// </summary>
    public sealed class AnonymousAsyncEnumerable<T> : AsyncEnumerableBase<T>
    {
        private readonly Func<CancellationToken, IAsyncEnumerator<T>> _getEnumerator;
        private readonly string _name;

        /// <summary>
        /// Initialize with a <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator(CancellationToken)"/> implementation.
        /// </summary>
        public AnonymousAsyncEnumerable(
            Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator,
            [CallerMemberName] string name = default)
        {
            _getEnumerator = getEnumerator ?? throw new ArgumentNullException(nameof(getEnumerator));
            _name = name;
        }

        /// <inheritdoc />
        public override IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => _getEnumerator(token);

        /// <inheritdoc />
        public override string ToString() => _name ?? nameof(AnonymousAsyncEnumerable<T>);
    }
}
