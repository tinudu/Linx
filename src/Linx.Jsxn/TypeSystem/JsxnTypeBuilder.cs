namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// Base class for a <see cref="IJsxnType"/> that is a type builder rather than a type.
    /// </summary>
    public abstract class JsxnTypeBuilder : IJsxnType
    {
        private ArrayTypeBuilder _array;

        /// <summary>
        /// Gets the <see cref="ArrayTypeBuilder"/> with this builder as the <see cref="ArrayTypeBuilder.ElementType"/>.
        /// </summary>
        public ArrayTypeBuilder Array => _array ?? (_array = new ArrayTypeBuilder(this));

        IArrayType IJsxnType.Array => Array;

        internal JsxnTypeBuilder() { }

        /// <summary>
        /// Gets a string representation of the type.
        /// </summary>
        public abstract override string ToString();
    }
}
