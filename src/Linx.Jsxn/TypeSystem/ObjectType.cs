namespace Linx.Jsxn.TypeSystem
{
    using System.Collections.Generic;
    using Collections;

    /// <summary>
    /// An object type.
    /// </summary>
    public sealed class ObjectType : NonNullableType, INamedType
    {
        /// <summary>
        /// The <see cref="ObjectType"/> with no properties, which also is the base type of all other types.
        /// </summary>
        public static ObjectType Object { get; } = new ObjectType();

        /// <summary>
        /// The modifier.
        /// </summary>
        public ObjectModifier Modifier { get; }

        /// <summary>
        /// The name.
        /// </summary>
        public Identifier Name { get; }

        /// <summary>
        /// The base type.
        /// </summary>
        public ObjectType BaseType { get; internal set; }

        /// <summary>
        /// Gets the set of implemented interfaces.
        /// </summary>
        public IReadOnlyCollection<InterfaceType> Interfaces { get; internal set; }

        /// <summary>
        /// Gets the set the properties.
        /// </summary>
        public IReadOnlyDictionary<Identifier, JsxnType> Properties { get; internal set; }

        private ObjectType()
        {
            Modifier = ObjectModifier.Virtual;
            Name = (Identifier)"object";
            Interfaces = new InterfaceType[0];
            Properties = new Dictionary<Identifier, JsxnType>().ReadOnly();
        }

        internal ObjectType(Identifier name)
        {
            Name = name;
        }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
