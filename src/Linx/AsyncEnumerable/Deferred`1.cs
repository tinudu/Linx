using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable
{
    /// <summary>
    /// Proxy to retrieve a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// If this type is the type parameter of a <see cref="IAsyncEnumerator{T}"/>,
    /// the instance exposed in <see cref="IAsyncEnumerator{T}.Current"/>
    /// when <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> returned true allows
    /// the result to be retrieved at most once before the next call to
    /// <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> or <see cref="IAsyncDisposable.DisposeAsync"/>.
    /// </remarks>
    public struct Deferred<T>
    {
        internal interface IProvider
        {
            T GetResult(short version);
        }

        private readonly IProvider _provider;
        private readonly short _version;

        internal Deferred(IProvider provider, short version)
        {
            _provider = provider;
            _version = version;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Deferred{T}"/> has expired.</exception>
        public T GetResult() => _provider is null ? default : _provider.GetResult(_version);
    }
}
