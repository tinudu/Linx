namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Interface for a <see cref="JsxnType"/> or a <see cref="JsxnTypeBuilder"/>.
    /// </summary>
    public interface IJsxnType
    {
        /// <summary>
        /// Gets the <see cref="IArrayType"/> having this type as the <see cref="IArrayType.ElementType"/>.
        /// </summary>
        IArrayType Array { get; }
    }
}
