namespace Linx.Jsxn.Schema
{
    using System.Collections.Generic;

    /// <summary>
    /// An interface type.
    /// </summary>
    public sealed class InterfaceType : NamedType
    {
        /// <inheritdoc />
        public override Identifier Name { get; }

        /// <summary>
        /// Interfaces imlemented by this interface.
        /// </summary>
        public IReadOnlyCollection<InterfaceType> Implements { get; internal set; }

        internal PropertyBag<Property> PropertyBag { get; set; }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IReadOnlyDictionary<string, Property> Properties => PropertyBag;

        internal InterfaceType(Identifier name) => Name = name;
    }
}
