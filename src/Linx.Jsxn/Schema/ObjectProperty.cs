namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// A <see cref="Property"/> of an interface.
    /// </summary>
    public sealed class ObjectProperty : Property
    {
        /// <summary>
        /// Gets whether the property is calculated and thus not serialized.
        /// </summary>
        public bool IsCalculated { get; }

        internal ObjectProperty(Identifier name, JsxnType type, bool isCalculated) : base(name, type) => IsCalculated = isCalculated;
    }
}
