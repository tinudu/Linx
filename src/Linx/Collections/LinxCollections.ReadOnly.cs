namespace Linx.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    partial class LinxCollections
    {
        /// <summary>
        /// Gets a read only wrapper around the specified dictionary.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> ReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) => new ReadOnlyDictionary<TKey, TValue>(dictionary ?? throw new ArgumentNullException(nameof(dictionary)));

        private sealed class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> _wrapped;

            public ReadOnlyDictionary(IDictionary<TKey, TValue> wrapped) => _wrapped = wrapped;

            public int Count => _wrapped.Count;
            public IEnumerable<TKey> Keys => _wrapped.Keys;
            public IEnumerable<TValue> Values => _wrapped.Values;
            public TValue this[TKey key] => _wrapped[key];
            public bool ContainsKey(TKey key) => _wrapped.ContainsKey(key);
            public bool TryGetValue(TKey key, out TValue value) => _wrapped.TryGetValue(key, out value);
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _wrapped.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _wrapped.GetEnumerator();
        }
    }
}
