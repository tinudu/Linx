﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    internal sealed class AnonymousAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Func<CancellationToken, IAsyncEnumerator<T>> _getEnumerator;
        private readonly string _name;

        public AnonymousAsyncEnumerable(Func<CancellationToken, IAsyncEnumerator<T>> getEnumerator, string name)
        {
            Debug.Assert(getEnumerator != null);
            _getEnumerator = getEnumerator;
            _name = name ?? typeof(AnonymousAsyncEnumerable<T>).Name;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            => token.IsCancellationRequested ?
                new LinxAsyncEnumerable.ThrowIterator<T>(new OperationCanceledException(token)) :
                _getEnumerator(token);

        public override string ToString() => _name;

        public AnonymousAsyncEnumerable<T> WithName(string name) => name == _name ? this : new AnonymousAsyncEnumerable<T>(_getEnumerator, name);
    }
}
