namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Collections;
    using Enumerable;

    /// <summary>
    /// A structural type.
    /// </summary>
    public sealed class StructuralType : NonNullableType, IReadOnlyDictionary<Identifier, JsxnType>
    {
        private static readonly Dictionary<IEnumerable<KeyValuePair<Identifier, JsxnType>>, StructuralType> _instances = new Dictionary<IEnumerable<KeyValuePair<Identifier, JsxnType>>, StructuralType>(SequenceComparer<KeyValuePair<Identifier, JsxnType>>.Default);

        /// <summary>
        /// Gets the type with the specified signature.
        /// </summary>
        public static StructuralType GetType(IEnumerable<KeyValuePair<Identifier, JsxnType>> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var list = new List<KeyValuePair<Identifier, JsxnType>>();
            foreach (var property in properties)
            {
                if (property.Key.Name == null) throw new ArgumentException("Property name undefined.");
                if (property.Value == null) throw new ArgumentException("Property value undefined.");
                list.Add(property);
            }

            lock (_instances)
            {
                if (_instances.TryGetValue(list, out var result)) return result;
                result = new StructuralType(list);
                _instances.Add(list, result);
                return result;
            }
        }

        private readonly List<KeyValuePair<Identifier, JsxnType>> _properties;
        private readonly Dictionary<Identifier, JsxnType> _propertiesByName;
        private readonly string _toString;

        private StructuralType(List<KeyValuePair<Identifier, JsxnType>> properties)
        {
            _properties = properties;
            _propertiesByName = properties.ToDictionary(p => p.Key, p => p.Value);
            _toString = $"{{ {string.Join(", ", properties.Select(p => $"{p.Key}: {p.Value}"))} }}";
            Keys = properties.Select(p => p.Key);
            Values = properties.Select(p => p.Value);
        }

        /// <inheritdoc />
        public int Count => _properties.Count;

        /// <inheritdoc />
        public IEnumerable<Identifier> Keys { get; }

        /// <inheritdoc />
        public IEnumerable<JsxnType> Values { get; }

        /// <inheritdoc />
        public JsxnType this[Identifier key] => _propertiesByName[key];

        /// <inheritdoc />
        public bool ContainsKey(Identifier key) => _propertiesByName.ContainsKey(key);

        /// <inheritdoc />
        public bool TryGetValue(Identifier key, out JsxnType value) => _propertiesByName.TryGetValue(key, out value);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<Identifier, JsxnType>> GetEnumerator() => _properties.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public override string ToString() => _toString;
    }
}
