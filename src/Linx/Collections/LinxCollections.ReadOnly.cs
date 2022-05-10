namespace Linx.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;

    partial class LinxCollections
    {
        /// <summary>
        /// Gets a read only wrapper around the specified dictionary.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> ReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new ReadOnlyDictionary<TKey, TValue>(dictionary ?? throw new ArgumentNullException(nameof(dictionary)));

        /// <summary>
        /// Gets a read only wrapper around the specified <see cref="KeyedCollection{TKey,TItem}"/>.
        /// </summary>
        public static IReadOnlyKeyedCollection<TKey, TItem> ReadOnly<TKey, TItem>(this KeyedCollection<TKey, TItem> keyedCollection) where TKey : notnull
        {
            if (keyedCollection == null) throw new ArgumentNullException(nameof(keyedCollection));
            return new ReadOnlyKeyedCollection<TKey, TItem>(keyedCollection);
        }

        private sealed class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> _wrapped;

            public ReadOnlyDictionary(IDictionary<TKey, TValue> wrapped) => _wrapped = wrapped;

            public int Count => _wrapped.Count;
            public IEnumerable<TKey> Keys => _wrapped.Keys;
            public IEnumerable<TValue> Values => _wrapped.Values;
            public TValue this[TKey key] => _wrapped[key];
            public bool ContainsKey(TKey key) => _wrapped.ContainsKey(key);
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _wrapped.TryGetValue(key, out value);
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _wrapped.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _wrapped.GetEnumerator();
        }

        private sealed class ReadOnlyKeyedCollection<TKey, TItem> : IReadOnlyKeyedCollection<TKey, TItem> where TKey : notnull
        {
            private readonly KeyedCollection<TKey, TItem> _wrapped;

            public ReadOnlyKeyedCollection(KeyedCollection<TKey, TItem> wrapped) => _wrapped = wrapped;

            public int Count => _wrapped.Count;
            public TItem this[int index] => _wrapped[index];
            public TItem this[TKey key] => _wrapped[key];
            public bool Contains(TKey key) => _wrapped.Contains(key);
            public IEnumerator<TItem> GetEnumerator() => _wrapped.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _wrapped.GetEnumerator();
        }
    }
}
