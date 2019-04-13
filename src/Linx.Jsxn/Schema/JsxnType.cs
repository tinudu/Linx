namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// Base class for all Jsxn types.
    /// </summary>
    public abstract class JsxnType
    {
        private ArrayType _array;

        /// <summary>
        /// Gets the <see cref="ArrayType"/> with this type as the <see cref="ArrayType.ElementType"/>.
        /// </summary>
        public ArrayType Array => _array ?? (_array = new ArrayType(this));

        internal JsxnType() { }

        /// <inheritdoc />
        public abstract override string ToString();
    }
}
