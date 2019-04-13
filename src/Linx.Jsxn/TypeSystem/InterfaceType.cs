namespace Linx.Jsxn.TypeSystem
{
    using System.Collections.Generic;

    /// <summary>
    /// An interface type.
    /// </summary>
    public sealed class InterfaceType : NonNullableType, IInterfaceType
    {
        /// <inheritdoc />
        public Identifier Name { get; }

        /// <summary>
        /// Gets the interfaces.
        /// </summary>
        public IReadOnlyCollection<InterfaceType> Interfaces { get; internal set; }

        /// <summary>
        /// Gets the properties.
        /// </summary>
        public IReadOnlyDictionary<Identifier,JsxnType> Properties { get; internal set; }

        internal InterfaceType(Identifier name) => Name = name;

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
