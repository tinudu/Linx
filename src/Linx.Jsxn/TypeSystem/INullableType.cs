namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Interface for a <see cref="NullableType"/> or a <see cref="NonNullableTypeBuilder"/>.
    /// </summary>
    public interface INullableType : IJsxnType
    {
        /// <summary>
        /// Gets the underlying <see cref="INonNullableType"/>.
        /// </summary>
        INonNullableType UnderlyingType { get; }
    }
}
