namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// An array type.
    /// </summary>
    public sealed class ArrayType : NonNullableType
    {
        /// <summary>
        /// Gets the <see cref="JsxnType"/> that is the array's element type.
        /// </summary>
        public JsxnType ElementType { get; }

        internal ArrayType(JsxnType elementType) => ElementType = elementType;

        /// <inheritdoc />
        public override string ToString() => ElementType + "[]";
    }
}
