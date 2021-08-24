namespace Linx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Static <see cref="KeyValuePair{TKey,TValue}"/> methods.
    /// </summary>
    [DebuggerNonUserCode]
    public static class KeyValuePair
    {
        /// <summary>
        /// Create a new <see cref="KeyValuePair{TKey,TValue}"/> from the specified values.
        /// </summary>
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) => new(key, value);

        /// <summary>
        /// Get a <see cref="IEqualityComparer{T}"/> for <see cref="KeyValuePair{TKey,TValue}"/> using the specified individual comparers for <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.
        /// </summary>
        public static IEqualityComparer<KeyValuePair<TKey, TValue>> GetEqualityComparer<TKey, TValue>(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) => KeyValueEqualityComparer<TKey, TValue>.Create(keyComparer, valueComparer);

        private sealed class KeyValueEqualityComparer<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>>
        {
            public static IEqualityComparer<KeyValuePair<TKey, TValue>> Create(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                if (keyComparer == null) keyComparer = EqualityComparer<TKey>.Default;
                if (valueComparer == null) valueComparer = EqualityComparer<TValue>.Default;
                return keyComparer == EqualityComparer<TKey>.Default && valueComparer == EqualityComparer<TValue>.Default
                    ? (IEqualityComparer<KeyValuePair<TKey, TValue>>)EqualityComparer<KeyValuePair<TKey, TValue>>.Default
                    : new KeyValueEqualityComparer<TKey, TValue>(keyComparer, valueComparer);
            }

            private readonly IEqualityComparer<TKey> _keyComparer;
            private readonly IEqualityComparer<TValue> _valueComparer;

            private KeyValueEqualityComparer(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                _keyComparer = keyComparer;
                _valueComparer = valueComparer;
            }

            public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => _keyComparer.Equals(x.Key, y.Key) && _valueComparer.Equals(x.Value, y.Value);
            public int GetHashCode(KeyValuePair<TKey, TValue> obj) => HashCode.Combine(_keyComparer.GetHashCode(obj.Key), _valueComparer.GetHashCode(obj.Value));
        }
    }
}
