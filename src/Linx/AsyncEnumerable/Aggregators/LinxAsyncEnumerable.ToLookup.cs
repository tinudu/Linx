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

            var lookup = new Lookup<TKey, TSource>(comparer);
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                lookup.Add(keySelector(item), item);
            return lookup;
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

            var lookup = new Lookup<TKey, TElement>(comparer);
            await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                lookup.Add(keySelector(item), elementSelector(item));
            return lookup;
        }

        // have to re-implement that because Lookup<,> has no public constructor
        private sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>
        {
            private readonly Dictionary<Boxed<TKey>, Grouping> _groupings;

            public Lookup(IEqualityComparer<TKey> comparer) => _groupings = new Dictionary<Boxed<TKey>, Grouping>(Boxed.GetEqualityComparer(comparer));

            public int Count => _groupings.Count;

            public IEnumerable<TElement> this[TKey key] => _groupings.TryGetValue(key, out var elements) ? elements : Enumerable.Empty<TElement>();

            public bool Contains(TKey key) => _groupings.ContainsKey(key);

            public void Add(TKey key, TElement element)
            {
                var boxedKey = new Boxed<TKey>(key);
                if (_groupings.TryGetValue(boxedKey, out var g))
                    g.Add(element);
                else
                {
                    g = new Grouping(key, element);
                    _groupings.Add(boxedKey, g);
                }
            }

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => _groupings.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Grouping : IGrouping<TKey, TElement>, IReadOnlyList<TElement>
            {
                private TElement[] _elements;

                public TKey Key { get; }
                public int Count { get; private set; }
                public TElement this[int index] => index >= 0 && index < Count ? _elements[index] : throw new IndexOutOfRangeException();

                public Grouping(TKey key, TElement first)
                {
                    Key = key;
                    _elements = new[] { first };
                    Count = 1;
                }

                public void Add(TElement element)
                {
                    if (Count == _elements.Length)
                    {
                        var old = Linx.Exchange(ref _elements, new TElement[_elements.Length * 2]);
                        Array.Copy(old, 0, _elements, 0, old.Length);
                    }
                    _elements[Count++] = element;
                }

                public IEnumerator<TElement> GetEnumerator() => (_elements.Length == Count ? _elements : _elements.Take(Count)).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }
        }
    }
}
