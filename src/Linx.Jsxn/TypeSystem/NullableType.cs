namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// A nullable <see cref="JsxnType"/>.
    /// </summary>
    public sealed class NullableType : JsxnType, INullableType
    {
        /// <summary>
        /// Gets the underlying <see cref="NonNullableType"/>.
        /// </summary>
        public NonNullableType UnderlyingType { get; }

        INonNullableType INullableType.UnderlyingType => UnderlyingType;

        internal NullableType(NonNullableType underlyingType) => UnderlyingType = underlyingType;

        /// <inheritdoc />
        public override string ToString() => UnderlyingType + "?";
    }
}
