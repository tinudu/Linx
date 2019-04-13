namespace Linx.Jsxn.Schema
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Dictionary with predictable enumeration order behaviour.
    /// </summary>
    internal sealed class PropertyBag<TProperty> : IReadOnlyDictionary<string, TProperty> where TProperty : Property
    {
        private readonly Dictionary<string, TProperty> _byPropertyName;
        private readonly IEnumerable<KeyValuePair<string, TProperty>> _items;

        public PropertyBag(IEnumerable<TProperty> properties, bool sort)
        {
            var list = properties.ToList();
            if (sort) list.Sort((x, y) => x.Name.CompareTo(y.Name));

            _byPropertyName = list.ToDictionary(p => p.Name.Name);
            _items = list.Select(p => new KeyValuePair<string, TProperty>(p.Name, p));
            Keys = list.Select(p => p.Name.Name);
            Values = list.Select(p => p);
        }

        public int Count => _byPropertyName.Count;
        public IEnumerable<string> Keys { get; }
        public IEnumerable<TProperty> Values { get; }
        public TProperty this[string key] => _byPropertyName[key];
        public bool ContainsKey(string key) => _byPropertyName.ContainsKey(key);
        public bool TryGetValue(string key, out TProperty value) => _byPropertyName.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, TProperty>> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
