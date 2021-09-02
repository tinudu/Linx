namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    internal sealed class AnonymousAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Func<CancellationToken, IAsyncEnumerator<T>> _getEnumerator;

        public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator)
        {
            Debug.Assert(getEnumerator != null);
            _getEnumerator = getEnumerator;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            => token.IsCancellationRequested ?
                new LinxAsyncEnumerable.ThrowIterator<T>(new OperationCanceledException(token)) :
                _getEnumerator(token);
    }
}
