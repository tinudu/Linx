namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Interface for a <see cref="NonNullableType"/> or a <see cref="NonNullableTypeBuilder"/>.
    /// </summary>
    public interface INonNullableType : IJsxnType
    {
        /// <summary>
        /// Gets the <see cref="INullableType"/> having this type as the <see cref="INullableType.UnderlyingType"/>.
        /// </summary>
        INullableType Nullable { get; }
    }
}
