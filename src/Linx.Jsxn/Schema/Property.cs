namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// A property.
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Property name.
        /// </summary>
        public Identifier Name { get; }

        /// <summary>
        /// Property type.
        /// </summary>
        public JsxnType Type { get; }

        internal Property(Identifier name, JsxnType type)
        {
            Name = name;
            Type = type;
        }
    }
}
