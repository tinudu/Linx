namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// A nullable <see cref="JsxnType"/>.
    /// </summary>
    public sealed class NullableType : JsxnType
    {
        /// <summary>
        /// Gets the underlying <see cref="NonNullableType"/>.
        /// </summary>
        public NonNullableType UnderlyingType { get; }

        internal NullableType(NonNullableType underlyingType) => UnderlyingType = underlyingType;

        /// <inheritdoc />
        public override string ToString() => UnderlyingType + "?";
    }
}
