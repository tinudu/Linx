namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Collections;
    using Enumerable;

    /// <summary>
    /// A structural type.
    /// </summary>
    public sealed class TupleType : NonNullableType
    {
        private static readonly Dictionary<IEnumerable<KeyValuePair<Identifier?, JsxnType>>, TupleType> _instances = new Dictionary<IEnumerable<KeyValuePair<Identifier?, JsxnType>>, TupleType>(SequenceComparer<KeyValuePair<Identifier?, JsxnType>>.Default);

        /// <summary>
        /// Gets the singleton matchinig the specified signature.
        /// </summary>
        public static TupleType GetType(IEnumerable<KeyValuePair<Identifier?, JsxnType>> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var propList = new List<KeyValuePair<Identifier?, JsxnType>>();
            foreach (var property in properties)
            {
                if (property.Key != null && property.Key.Value.Name == null) throw new ArgumentException("Identifier not set.");
                if (property.Value == null) throw new ArgumentException("Type not set.");
                propList.Add(property);
            }
            var propsRo = propList.AsReadOnly();
            lock (_instances)
            {
                if (_instances.TryGetValue(propsRo, out var result)) return result;
                result = new TupleType(propsRo);
                _instances.Add(propsRo, result);
                return result;
            }
        }

        private readonly Dictionary<Identifier, JsxnType> _propertiesByName;

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IReadOnlyList<KeyValuePair<Identifier?, JsxnType>> Properties { get; }

        private TupleType(IReadOnlyList<KeyValuePair<Identifier?, JsxnType>> properties)
        {
            Properties = properties;
            // ReSharper disable once PossibleInvalidOperationException
            _propertiesByName = properties.Where(p => p.Key != null).ToDictionary(p => p.Key.Value, p => p.Value);
        }

        /// <summary>
        /// Gets the type of the property with the specified name.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No such property.</exception>
        public JsxnType GetPropertyType(Identifier name) => _propertiesByName[name];

        /// <summary>
        /// Tries to get the type of the propert with the specified name name.
        /// </summary>
        /// <returns>The property's type or null.</returns>
        public JsxnType TryGetPropertyType(Identifier name) => _propertiesByName.TryGetValue(name, out var result) ? result : null;

        /// <inheritdoc />
        public override string ToString() => $"({string.Join(", ", Properties.Select(p => p.Key == null ? p.Value.ToString() : $"{p.Key.Value}:{p.Value}"))})";
    }
}
