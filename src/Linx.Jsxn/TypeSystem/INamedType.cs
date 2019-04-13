namespace Linx.Jsxn.TypeSystem
{
    /// <summary>
    /// A <see cref="INonNullableType"/> that is identified by it's name.
    /// </summary>
    public interface INamedType : INonNullableType
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        Identifier Name { get; }
    }
}
