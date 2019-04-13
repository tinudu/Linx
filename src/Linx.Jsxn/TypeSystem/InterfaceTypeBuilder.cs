namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Builder for an <see cref="InterfaceType"/>.
    /// </summary>
    public sealed class InterfaceTypeBuilder : NonNullableTypeBuilder, IInterfaceType
    {
        /// <inheritdoc />
        public Identifier Name { get; }

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
        public InterfaceTypeBuilder(Identifier name) => Name = name.Name != null ? name : throw new ArgumentNullException(nameof(name));

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
