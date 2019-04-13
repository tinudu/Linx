namespace Linx.Jsxn.Schema
{
    /// <summary>
    /// Modifier of an <see cref="ObjectType"/>.
    /// </summary>
    public enum ObjectModifier
    {
        /// <summary>
        /// The type is abstract, thus no instances can exist.
        /// </summary>
        Abstract,

        /// <summary>
        /// The type can be instanciated and derived from.
        /// </summary>
        Virtual,

        /// <summary>
        /// The type cannot be derived from.
        /// </summary>
        Sealed
    }
}
