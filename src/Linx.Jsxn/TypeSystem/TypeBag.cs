namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Container for a type system.
    /// </summary>
    public sealed class TypeBag
    {
        private readonly Dictionary<Identifier, JsxnType> _namedTypes = new Dictionary<Identifier, JsxnType>();

        /// <summary>
        /// Initialize with the specified <paramref name="types"/>.
        /// </summary>
        public TypeBag(IEnumerable<INamedType> types)
        {
            if (types == null) throw new ArgumentNullException(nameof(types));

            // add predefined types (primities and 'object')
            foreach (var primitive in PrimitiveType.Predefined) _namedTypes.Add(primitive.Name, primitive);
            _namedTypes.Add(ObjectType.Object.Name, ObjectType.Object);

            var resolved = _namedTypes.Values.ToDictionary<JsxnType, IJsxnType>(t => t);
            foreach (var type in types) Resolve(type);

            JsxnType Resolve(IJsxnType type)
            {
                if (type == null) throw new ArgumentException("Null type.");
                if (resolved.TryGetValue(type, out var result)) return result;
                switch (type)
                {
                    case NullableType nt:
                        Resolve(nt.UnderlyingType);
                        result = nt;
                        resolved.Add(type, result);
                        break;
                    case ArrayType at:
                        Resolve(at.ElementType);
                        result = at;
                        resolved.Add(type, result);
                        break;
                    case EnumType et:
                        _namedTypes.Add(et.Name, et);
                        result = et;
                        resolved.Add(type, result);
                        break;
                    case InterfaceType it:
                        _namedTypes.Add(it.Name, it);
                        result = it;
                        resolved.Add(type, result);
                        break;
                    case ObjectType ot:
                        _namedTypes.Add(ot.Name, ot);
                        result = ot;
                        resolved.Add(type, result);
                        break;
                    case NullableTypeBuilder ntb:
                        result = ((NonNullableType)Resolve(ntb.UnderlyingType)).Nullable;
                        resolved.Add(type, result);
                        break;
                    case ArrayTypeBuilder atb:
                        result = Resolve(atb).Array;
                        resolved.Add(type, result);
                        break;
                    case InterfaceTypeBuilder itb:
                        {
                            var t = new InterfaceType(itb.Name);
                            result = t;
                            resolved.Add(type, result);

                            var baseInterfaces = new HashSet<InterfaceType>();
                            foreach (var bib in itb.Interfaces)
                            {
                                if (bib == null) throw new ArgumentException($"Null interface on type '{t}'.");
                                var bi = (InterfaceType)Resolve(bib);
                                if (bi.Interfaces == null) throw new ArgumentException($"Circular reference involving types '{t}' and '{bi}'.");
                                if (!baseInterfaces.Add(bi)) continue;
                                foreach (var bbi in bi.Interfaces) baseInterfaces.Add(bbi);
                            }
                            var properties = InterfaceProperties(baseInterfaces);
                            foreach (var p in itb.Properties)
                            {
                                if (p.Key.Name == null) throw new ArgumentException($"Property name not defined on '{t.Name}'.");
                                if (p.Value == null) throw new ArgumentException($"Property type not defined on '{t.Name}.{p.Key}'.");
                                var propertyType = Resolve(p.Value);
                                if (properties.TryGetValue(p.Key, out var baseType))
                                {
                                    if (baseType.IsAssignableTo(propertyType)) properties[p.Key] = propertyType;
                                    throw new ArgumentException($"'{t.Name}.{p.Key}' '{propertyType}' is not compatible with ''.");
                                }
                            }

                            t.Interfaces = baseInterfaces;
                            t.Properties = new PropertyBag(properties);
                        }
                        break;
                    case ObjectTypeBuilder otb:
                        {
                            var t = new ObjectType(otb.Name);
                            result = t;
                            resolved.Add(type, result);

                            var baseType = otb.BaseType != null ? (ObjectType)Resolve(otb.BaseType) : ObjectType.Object;
                            if (baseType.Interfaces == null) throw new ArgumentException($"Circular reference involving types '{t}' and '{baseType}'.");

                            var baseInterfaces = new HashSet<InterfaceType>(baseType.Interfaces);
                            foreach (var bib in otb.Interfaces)
                            {
                                if (bib == null) throw new ArgumentException($"Null interface on type '{t}'.");
                                var bi = (InterfaceType)Resolve(bib);
                                if (bi.Interfaces == null) throw new ArgumentException($"Circular reference involving types '{t}' and '{bi}'.");
                                if (!baseInterfaces.Add(bi)) continue;
                                foreach (var bbi in bi.Interfaces) baseInterfaces.Add(bbi);
                            }

                            t.BaseType = baseType;
                            t.Interfaces = baseInterfaces;
                        }
                        throw new NotImplementedException();
                    default:
                        throw new NotSupportedException(type.GetType().FullName);
                }
                return result;
            }
        }

        private static Dictionary<Identifier, JsxnType> InterfaceProperties(IEnumerable<InterfaceType> baseInterfaces)
            => baseInterfaces
            .SelectMany(i => i.Properties)
            .GroupBy(p => p.Key, (k, vs) => new KeyValuePair<Identifier, JsxnType>(k, MostSpecificType(vs.Select(v => v.Value))))
            .ToDictionary(p => p.Key, p => p.Value);

        private static JsxnType MostSpecificType(IEnumerable<JsxnType> types) => types.Aggregate((JsxnType)ObjectType.Object, (a, c) => a.IsAssignableTo(c) ? c : c.IsAssignableTo(a) ? a : throw new InvalidOperationException());

        private sealed class PropertyBag : IReadOnlyDictionary<Identifier, JsxnType>
        {
            private readonly Dictionary<Identifier, JsxnType> _dictionary;
            private readonly List<KeyValuePair<Identifier, JsxnType>> _list;

            public PropertyBag(Dictionary<Identifier, JsxnType> dictionary)
            {
                _dictionary = dictionary;
                var list = _list = dictionary.ToList();
                list.Sort((x, y) => x.Key.CompareTo(y.Key));
                Keys = list.Select(p => p.Key);
                Values = list.Select(p => p.Value);
            }

            public int Count => _list.Count;
            public IEnumerable<Identifier> Keys { get; }
            public IEnumerable<JsxnType> Values { get; }
            public JsxnType this[Identifier key] => _dictionary[key];
            public bool ContainsKey(Identifier key) => _dictionary.ContainsKey(key);
            public bool TryGetValue(Identifier key, out JsxnType value) => _dictionary.TryGetValue(key, out value);
            public IEnumerator<KeyValuePair<Identifier, JsxnType>> GetEnumerator() => _list.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
