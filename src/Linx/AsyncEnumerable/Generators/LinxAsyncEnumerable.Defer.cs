namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a sequence that invokes the factory whenever it is enumerated.
        /// </summary>
        public static IAsyncEnumerable<T> Defer<T>(Func<IAsyncEnumerable<T>> getSource)
        {
            if (getSource == null) throw new ArgumentNullException(nameof(getSource));
            return new DeferAsyncEnumerable<T>(getSource);
        }

        /// <summary>
        /// Returns a sequence that invokes the factory whenever it is enumerated.
        /// </summary>
        public static IAsyncEnumerable<T> DeferAwait<T>(Func<CancellationToken, Task<IAsyncEnumerable<T>>> getSourceAsync)
        {
            if (getSourceAsync == null) throw new ArgumentNullException(nameof(getSourceAsync));
            return new DeferAwaitAsyncEnumerable<T>(getSourceAsync);
        }

        private sealed class DeferAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly Func<IAsyncEnumerable<T>> _getSource;

            public DeferAsyncEnumerable(Func<IAsyncEnumerable<T>> getSource)
            {
                Debug.Assert(getSource is not null);
                _getSource = getSource;
            }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                var source = _getSource();
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return item;
            }
        }

        private sealed class DeferAwaitAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly Func<CancellationToken, Task<IAsyncEnumerable<T>>> _getSourceAsync;

            public DeferAwaitAsyncEnumerable(Func<CancellationToken, Task<IAsyncEnumerable<T>>> getSourceAsync)
            {
                Debug.Assert(getSourceAsync is not null);
                _getSourceAsync = getSourceAsync;
            }

            public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
            {
                var source = await _getSourceAsync(token).ConfigureAwait(false);
                await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                    yield return item;
            }
        }
    }
}
