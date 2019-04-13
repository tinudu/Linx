namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Base class for a non-nullable <see cref="JsxnTypeBuilder"/>.
    /// </summary>
    public abstract class NonNullableTypeBuilder : JsxnTypeBuilder, INonNullableType
    {
        /// <summary>
        /// Gets the <see cref="NullableTypeBuilder"/> with this builder as the <see cref="NullableTypeBuilder.UnderlyingType"/>.
        /// </summary>
        public NullableTypeBuilder Nullable { get; }

        INullableType INonNullableType.Nullable => Nullable;

        internal NonNullableTypeBuilder() => Nullable = new NullableTypeBuilder(this);
    }
}
