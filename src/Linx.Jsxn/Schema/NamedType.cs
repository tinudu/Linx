namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// A type having a name.
    /// </summary>
    public abstract class NamedType : NonNullableType
    {
        /// <summary>
        /// Gets the type name.
        /// </summary>
        public abstract Identifier Name { get; }

        internal NamedType() { }

        /// <inheritdoc />
        public sealed override string ToString() => Name;
    }
}
