namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Group by a key.
        /// </summary>
        public static IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>> GroupBy<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
            => new GroupByEnumerable<TSource, TKey>(source, keySelector, false, keyComparer);

        /// <summary>
        /// Group by a key; close a group when unsubscribed.
        /// </summary>
        public static IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>> GroupByWhileObserved<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> keyComparer = null)
            => new GroupByEnumerable<TSource, TKey>(source, keySelector, true, keyComparer);

        private sealed class GroupByEnumerable<TSource, TKey> : IAsyncEnumerable<IGroupedAsyncEnumerable<TKey, TSource>>
        {
            private readonly IAsyncEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly bool _whileObserved;
            private readonly IEqualityComparer<Wrapped<TKey>> _keyComparer;

            public GroupByEnumerable(
                IAsyncEnumerable<TSource> source,
                Func<TSource, TKey> keySelector,
                bool whileObserved,
                IEqualityComparer<TKey> keyComparer)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
                _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
                _whileObserved = whileObserved;
                _keyComparer = Wrapped<TKey>.GetComparer(keyComparer);
            }

            public IAsyncEnumerator<IGroupedAsyncEnumerable<TKey, TSource>> GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private sealed class Enumerator : IAsyncEnumerator<IGroupedAsyncEnumerable<TKey, TSource>>
            {
                public Enumerator(GroupByEnumerable<TSource, TKey> enumerable, CancellationToken token) { }

                public IGroupedAsyncEnumerable<TKey, TSource> Current { get; private set; }

                public ICoroutineAwaiter<bool> MoveNextAsync(bool continueOnCapturedContext = false)
                {
                    throw new NotImplementedException();
                }

                public Task DisposeAsync()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
