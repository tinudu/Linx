namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// A nullable <see cref="JsxnTypeBuilder"/>.
    /// </summary>
    public sealed class NullableTypeBuilder : JsxnTypeBuilder, INullableType
    {
        /// <summary>
        /// Gets a reference to the underlying type.
        /// </summary>
        public NonNullableTypeBuilder UnderlyingType { get; }

        INonNullableType INullableType.UnderlyingType => UnderlyingType;

        internal NullableTypeBuilder(NonNullableTypeBuilder underlyingType) => UnderlyingType = underlyingType;

        /// <inheritdoc />
        public override string ToString() => UnderlyingType + "?";
    }
}
