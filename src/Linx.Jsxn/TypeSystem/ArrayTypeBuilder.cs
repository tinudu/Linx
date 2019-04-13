namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Builder for an array type.
    /// </summary>
    public sealed class ArrayTypeBuilder : NonNullableTypeBuilder, IArrayType
    {
        /// <summary>
        /// Gets the <see cref="JsxnTypeBuilder"/> that is the array's element type builder.
        /// </summary>
        public JsxnTypeBuilder ElementType { get; }

        IJsxnType IArrayType.ElementType => ElementType;

        internal ArrayTypeBuilder(JsxnTypeBuilder elementType) => ElementType = elementType;

        /// <inheritdoc />
        public override string ToString() => ElementType + "[]";
    }
}
