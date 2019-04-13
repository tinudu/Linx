namespace Linx.Jsxn.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Collections;
    using Enumerable;

    /// <summary>
    /// A structural <see cref="JsxnType"/>
    /// </summary>
    public sealed class StructuralType : NonNullableType
    {
        private static readonly Dictionary<IEnumerable<KeyValuePair<Identifier, JsxnType>>, StructuralType> _instances = new Dictionary<IEnumerable<KeyValuePair<Identifier, JsxnType>>, StructuralType>(SequenceComparer<KeyValuePair<Identifier, JsxnType>>.Default);

        /// <summary>
        /// Gets a singleton for the specified signature.
        /// </summary>
        public static StructuralType GetType(IEnumerable<KeyValuePair<Identifier, JsxnType>> properties)
        {
            var signature = properties.Select(p =>
            {
                if (p.Key.Name == null) throw new ArgumentException("Null name.");
                if (p.Value == null) throw new ArgumentException("Null type.");
                return p;
            }).ToList();
            lock (_instances)
            {
                if (_instances.TryGetValue(signature, out var result)) return result;
                result = new StructuralType(new PropertyBag<Property>(signature.Select(p => new Property(p.Key, p.Value)), false));
                _instances.Add(signature, result);
                return result;
            }
        }

        private readonly string _toString;

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IReadOnlyDictionary<string, Property> Properties { get; }

        private StructuralType(PropertyBag<Property> propertyBag)
        {
            Properties = propertyBag;
            _toString = $"{{ {string.Join(", ", propertyBag.Values.Select(p => $"{p.Name}: {p.Type}"))} }}";
        }

        /// <inheritdoc />
        public override string ToString() => _toString;
    }
}
