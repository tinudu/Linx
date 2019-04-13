namespace Linx.Expressions.Serialization.Parsing
{
    using System.Collections;
    using System.Collections.Generic;

    public sealed class KeywordToken : IToken
    {
        private static readonly ReadOnlyDictionary<string, KeywordToken> _byKeyword = new ReadOnlyDictionary<string, KeywordToken>();
        public static IReadOnlyDictionary<string, KeywordToken> ByKeyword { get; } = _byKeyword;

        private static readonly ReadOnlyDictionary<KeywordEnum, KeywordToken> _byKeywordEnum = new ReadOnlyDictionary<KeywordEnum, KeywordToken>();
        public static IReadOnlyDictionary<KeywordEnum, KeywordToken> ByKeywordEnum { get; } = _byKeywordEnum;

        public static KeywordToken False { get; } = new KeywordToken(KeywordEnum.False, "false");
        public static KeywordToken Null { get; } = new KeywordToken(KeywordEnum.Null, "null");
        public static KeywordToken True { get; } = new KeywordToken(KeywordEnum.True, "true");

        TokenType IToken.Type => TokenType.Keyword;
        public KeywordEnum Enum { get; }
        public string Keyword { get; }

        private KeywordToken(KeywordEnum @enum, string keyword)
        {
            Enum = @enum;
            Keyword = keyword;
            _byKeyword.Add(keyword, this);
            _byKeywordEnum.Add(@enum, this);
        }

        public override string ToString() => Keyword;

        private sealed class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            private readonly Dictionary<TKey, TValue> _wrapped = new Dictionary<TKey, TValue>();

            public void Add(TKey key, TValue value) => _wrapped.Add(key, value);

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
