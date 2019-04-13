namespace Linx.Jsxn.Schema
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Dictionary of <see cref="JsxnType"/>s.
    /// </summary>
    public sealed class JsxnSchema
    {
        /// <summary>
        /// Declared types by name.
        /// </summary>
        public IReadOnlyDictionary<string, NamedType> Types { get; }

        /// <summary>
        /// Initialize.
        /// </summary>
        public JsxnSchema(IEnumerable<JsxnSchemaBuilder> builders)
        {
            if (builders == null) throw new ArgumentNullException(nameof(builders));

            var types = PrimitiveType.Defined.Concat(new NamedType[] { ObjectType.Object }).ToDictionary(t => t.Name, t => (SchemaType: default(JsxnSchemaBuilder.ISchemaType), Type: t));
            foreach (var t in builders.SelectMany(sch => sch.Types))
            {
                if (types.ContainsKey(t.Name)) throw new ArgumentException($"Duplicate definition of type '{t.Name}'.");
                types.Add(t.Name, (t, t as NamedType));
            }

            foreach (var typeName in types.Keys) ResolveNamed(typeName);
            Types = types.ToDictionary(t => t.Key.Name, t => t.Value.Type);

            NamedType ResolveNamed(Identifier typeName)
            {
                if (!types.TryGetValue(typeName, out var tt)) throw new ArgumentException($"No type '{typeName}'.");
                if (tt.Type != null) return tt.Type;
                switch (tt.SchemaType)
                {
                    case JsxnSchemaBuilder.InterfaceType it:
                        {
                            var type = new InterfaceType(it.Name);
                            tt.Type = type;
                            var implements = new HashSet<InterfaceType>();
                            foreach (var iName in it.Implements)
                            {
                                var implemented = (InterfaceType)ResolveNamed(iName);
                                if (implemented.Implements == null) throw new ArgumentException($"Circular reference involving types '{type}' and '{implemented}'.");
                                if (!implements.Add(implemented)) continue;
                                foreach (var i in implemented.Implements) implements.Add(i);
                            }
                            type.Implements = implements.ToList().AsReadOnly();
                            type.PropertyBag = new PropertyBag<Property>(it.Properties.Select(p => new Property(p.Name, ResolveRef(p.TypeRef))), true);
                            return type;
                        }
                    case JsxnSchemaBuilder.ObjectType ot:
                        {
                            var type = new ObjectType(ot.Name, ot.Modifier);
                            tt.Type = type;

                            var extends = (ObjectType)ResolveNamed(ot.Extends);
                            if (extends.Implements == null) throw new ArgumentException($"Circular reference involving types '{type}' and '{extends}'.");
                            if (extends.Modifier == ObjectModifier.Sealed) throw new ArgumentException($"'{type}' cannot extend '{extends}' because it's sealed.");
                            type.Extends = extends;

                            var implements = new HashSet<InterfaceType>(extends.Implements);
                            foreach (var iName in ot.Implements)
                            {
                                var implemented = (InterfaceType)ResolveNamed(iName);
                                if (implemented.Implements == null) throw new ArgumentException($"Circular reference involving types '{type}' and '{implemented}'.");
                                if (!implements.Add(implemented)) continue;
                                foreach (var i in implemented.Implements) implements.Add(i);
                            }
                            type.Implements = implements.ToList().AsReadOnly();

                            var props = extends.Properties.ToDictionary(p => p.Key, p => p.Value);
                            foreach (var p in ot.Properties) props[p.Name] = new ObjectProperty(p.Name, ResolveRef(p.TypeRef), p.IsCalculated);
                            type.PropertyBag = new PropertyBag<ObjectProperty>(props.Values, true);
                            return type;
                        }
                    default:
                        throw new Exception(tt.SchemaType + "???");
                }
            }

            JsxnType ResolveRef(string typeRef)
            {
                if (typeRef.EndsWith("[]")) return ResolveRef(typeRef.Substring(0, typeRef.Length - 2)).Array;
                if (typeRef.EndsWith("?")) return ((NonNullableType)ResolveRef(typeRef.Substring(0, typeRef.Length - 1))).Nullable;
                return ResolveNamed((Identifier)typeRef);
            }
        }
    }
}
