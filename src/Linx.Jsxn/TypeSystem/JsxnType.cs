namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Base class for a <see cref="IJsxnType"/> that is a type rather than a type builder.
    /// </summary>
    public abstract class JsxnType : IJsxnType
    {
        private ArrayType _array;

        /// <summary>
        /// Gets the <see cref="ArrayType"/> with this type as the <see cref="ArrayType.ElementType"/>.
        /// </summary>
        public ArrayType Array => _array ?? (_array = new ArrayType(this));

        IArrayType IJsxnType.Array => Array;

        internal JsxnType() { }

        /// <summary>
        /// Gets a string representation of the type.
        /// </summary>
        public abstract override string ToString();
    }
}
