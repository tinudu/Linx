namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                sources.Concat() :
                Create(token => new MergeEnumerator<T>(sources, maxConcurrent, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent = int.MaxValue)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (maxConcurrent <= 0) throw new ArgumentOutOfRangeException(nameof(maxConcurrent));

            return maxConcurrent == 1 ?
                sources.Concat() :
                Create(token => new MergeEnumerator<T>(sources.Async(), maxConcurrent, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            return Create(token => new MergeEnumerator<T>(new[] { first, second }.Async(), int.MaxValue, token));
        }

        /// <summary>
        /// Merges multiple sequences into one.
        /// </summary>
        public static IAsyncEnumerable<T> Merge<T>(this IAsyncEnumerable<T> first, IAsyncEnumerable<T> second, params IAsyncEnumerable<T>[] sources)
        {
            if (first == null) throw new ArgumentNullException(nameof(first));
            if (second == null) throw new ArgumentNullException(nameof(second));
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            return Create(token => new MergeEnumerator<T>(sources.Prepend(second).Prepend(first).Async(), int.MaxValue, token));
        }

        private sealed class MergeEnumerator<T> : IAsyncEnumerator<T>
        {
            public MergeEnumerator(IAsyncEnumerable<IAsyncEnumerable<T>> sources, int maxConcurrent, CancellationToken token)
            {
                throw new NotImplementedException();
            }

            public T Current => throw new NotImplementedException();
            public ValueTask<bool> MoveNextAsync() => throw new NotImplementedException();
            public ValueTask DisposeAsync() => throw new NotImplementedException();
        }
    }
}
