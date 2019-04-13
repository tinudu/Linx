namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// An array type.
    /// </summary>
    public sealed class ArrayType : NonNullableType, IArrayType
    {
        /// <summary>
        /// Gets the <see cref="JsxnType"/> that is the array's element type.
        /// </summary>
        public JsxnType ElementType { get; }

        IJsxnType IArrayType.ElementType => ElementType;

        internal ArrayType(JsxnType elementType) => ElementType = elementType;

        /// <inheritdoc />
        public override string ToString() => ElementType + "[]";
    }
}
