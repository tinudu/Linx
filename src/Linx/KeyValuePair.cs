namespace Linx
{
    using System.Collections.Generic;

    /// <summary>
    /// Static <see cref="KeyValuePair{TKey,TValue}"/> methods.
    /// </summary>
    public static class KeyValuePair
    {
        /// <summary>
        /// Create a new <see cref="KeyValuePair{TKey,TValue}"/> from the specified values.
        /// </summary>
        public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) => new KeyValuePair<TKey, TValue>(key, value);

        /// <summary>
        /// Create an equality comparer by specifying individual comparers for <typeparamref name="TKey"/> and <typeparamref name="TValue"/>.
        /// </summary>
        public static IEqualityComparer<KeyValuePair<TKey, TValue>> GetComparer<TKey, TValue>(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) => new KeyValueEqualityComparerImpl<TKey, TValue>(keyComparer, valueComparer);

        private sealed class KeyValueEqualityComparerImpl<TKey, TValue> : IEqualityComparer<KeyValuePair<TKey, TValue>>
        {
            private readonly IEqualityComparer<TKey> _keyComparer;
            private readonly IEqualityComparer<TValue> _valueComparer;

            public KeyValueEqualityComparerImpl(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
            {
                _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
                _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            }

            public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) => _keyComparer.Equals(x.Key, y.Key) && _valueComparer.Equals(x.Value, y.Value);
            public int GetHashCode(KeyValuePair<TKey, TValue> obj) => _keyComparer.GetHashCode(obj.Key) ^ ~_valueComparer.GetHashCode(obj.Value);
        }


    }
}
