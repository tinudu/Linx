namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static async Task<ILookup<TKey, TSource>> ToLookup<TSource, TKey>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            CancellationToken token,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var lookup = new LookupBuilder<TKey, TSource>(comparer);
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    lookup.Add(keySelector(current), current);
                }
                return lookup;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Creates a <see cref="ILookup{TKey, TElement}"/> from a <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        public static async Task<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            CancellationToken token,
            IEqualityComparer<TKey> comparer = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var lookup = new LookupBuilder<TKey, TElement>(comparer);
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    lookup.Add(keySelector(current), elementSelector(current));
                }
                return lookup;
            }
            finally { await ae.DisposeAsync(); }
        }

        // have to re-implement that because Lookup<,> has no public constructor
        private sealed class LookupBuilder<TKey, TElement> : ILookup<TKey, TElement>
        {
            private readonly Dictionary<Wrapped<TKey>, Grouping> _groupings;

            public LookupBuilder(IEqualityComparer<TKey> comparer) => _groupings = new Dictionary<Wrapped<TKey>, Grouping>(Wrapped<TKey>.GetComparer(comparer));

            public int Count => _groupings.Count;
            public IEnumerable<TElement> this[TKey key] => GetGrouping(key);

            public bool Contains(TKey key) => _groupings.ContainsKey(new Wrapped<TKey>(key));

            public void Add(TKey key, TElement element) => GetGrouping(key).Add(element);

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                foreach (var g in _groupings.Values)
                    if (g.Count > 0)
                        yield return g;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private Grouping GetGrouping(TKey key)
            {
                var wKey = new Wrapped<TKey>(key);
                if (_groupings.TryGetValue(wKey, out var g)) return g;
                g = new Grouping(key);
                _groupings.Add(wKey, g);
                return g;
            }

            private sealed class Grouping : IGrouping<TKey, TElement>
            {
                private readonly List<TElement> _elements = new List<TElement>();

                public TKey Key { get; }
                public int Count => _elements.Count;

                public Grouping(TKey key) => Key = key;

                public void Add(TElement element) => _elements.Add(element);
                public IEnumerator<TElement> GetEnumerator() => _elements.GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();
            }
        }
    }
}
