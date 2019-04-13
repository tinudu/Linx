namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Interface for a <see cref="ArrayType"/> or a <see cref="ArrayTypeBuilder"/>.
    /// </summary>
    public interface IArrayType : INonNullableType
    {
        /// <summary>
        /// Gets the <see cref="IJsxnType"/> that is the array's element type.
        /// </summary>
        IJsxnType ElementType { get; }
    }
}
