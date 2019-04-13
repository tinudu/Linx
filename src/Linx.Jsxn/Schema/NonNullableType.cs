namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// Base class for a non-nullable <see cref="JsxnType"/>.
    /// </summary>
    public abstract class NonNullableType : JsxnType
    {
        /// <summary>
        /// Gets the <see cref="NullableType"/> with this type as the <see cref="NullableType.UnderlyingType"/>.
        /// </summary>
        public NullableType Nullable { get; }

        internal NonNullableType() => Nullable = new NullableType(this);
    }
}
