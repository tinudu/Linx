namespace Linx.Jsxn.Schema
{
    using System.Collections.Generic;

    /// <summary>
    /// An object <see cref="JsxnType"/>.
    /// </summary>
    public sealed class ObjectType : NamedType
    {
        /// <summary>
        /// Gets the "object" <see cref="ObjectType"/>.
        /// </summary>
        public static ObjectType Object { get; } = new ObjectType((Identifier)"object", ObjectModifier.Virtual) { Implements = new InterfaceType[0], PropertyBag = new PropertyBag<ObjectProperty>(new ObjectProperty[0], false) };

        /// <inheritdoc />
        public override Identifier Name { get; }

        /// <summary>
        /// Gets the modifier.
        /// </summary>
        public ObjectModifier Modifier { get; }

        /// <summary>
        /// Gets the <see cref="ObjectType"/> that this type inherits from.
        /// </summary>
        public ObjectType Extends { get; internal set; }

        /// <summary>
        /// Interfaces imlemented by this interface.
        /// </summary>
        public IReadOnlyCollection<InterfaceType> Implements { get; internal set; }

        internal PropertyBag<ObjectProperty> PropertyBag { get; set; }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IReadOnlyDictionary<string, ObjectProperty> Properties => PropertyBag;

        internal ObjectType(Identifier name, ObjectModifier modifier)
        {
            Name = name;
            Modifier = modifier;
        }
    }
}
