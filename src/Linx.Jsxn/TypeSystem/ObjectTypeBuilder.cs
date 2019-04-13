namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Builder for an <see cref="ObjectType"/>.
    /// </summary>
    public sealed class ObjectTypeBuilder : NonNullableTypeBuilder, IObjectType
    {
        /// <inheritdoc />
        public Identifier Name { get; }

        /// <summary>
        /// Gets or sets the base type.
        /// </summary>
        public IObjectType BaseType { get; set; }

        /// <summary>
        /// Gets the base interfaces.
        /// </summary>
        public ISet<IInterfaceType> Interfaces { get; } = new HashSet<IInterfaceType>();

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IDictionary<Identifier, IJsxnType> Properties { get; } = new Dictionary<Identifier, IJsxnType>();

        /// <summary>
        /// Initialize with the specified <paramref name="name"/>.
        /// </summary>
        public ObjectTypeBuilder(Identifier name) => Name = name.Name != null ? name : throw new ArgumentNullException(nameof(name));

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
