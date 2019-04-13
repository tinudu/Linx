namespace Linx.Jsxn.Schema
{
    using System.Collections.Generic;

    /// <summary>
    /// A predefined primitive type.
    /// </summary>
    public sealed class PrimitiveType : NamedType
    {
        /// <summary>
        /// "bool" primitive type.
        /// </summary>
        public static PrimitiveType Boolean { get; } = new PrimitiveType((Identifier) "bool");

        /// <summary>
        /// "int" primitive type.
        /// </summary>
        public static PrimitiveType Int32 { get; } = new PrimitiveType((Identifier) "int");

        /// <summary>
        /// "long" primitive type.
        /// </summary>
        public static PrimitiveType Int64 { get; } = new PrimitiveType((Identifier) "long");

        /// <summary>
        /// "float" primitive type.
        /// </summary>
        public static PrimitiveType Single { get; } = new PrimitiveType((Identifier) "float");

        /// <summary>
        /// "double" primitive type.
        /// </summary>
        public static PrimitiveType Double { get; } = new PrimitiveType((Identifier) "double");

        /// <summary>
        /// "decimal" primitive type.
        /// </summary>
        public static PrimitiveType Decimal { get; } = new PrimitiveType((Identifier) "decimal");

        /// <summary>
        /// "string" primitive type.
        /// </summary>
        public static PrimitiveType String { get; } = new PrimitiveType((Identifier) "string");

        /// <summary>
        /// "guid" primitive type.
        /// </summary>
        public static PrimitiveType Guid { get; } = new PrimitiveType((Identifier) "guid");

        /// <summary>
        /// "dt" primitive type.
        /// </summary>
        public static PrimitiveType DateTime { get; } = new PrimitiveType((Identifier) "dt");

        /// <summary>
        /// "dto" primitive type.
        /// </summary>
        public static PrimitiveType DateTimeOffset { get; } = new PrimitiveType((Identifier) "dto");

        /// <summary>
        /// "ts" primitive type.
        /// </summary>
        public static PrimitiveType TimeSpan { get; } = new PrimitiveType((Identifier) "ts");

        /// <summary>
        /// "binary" primitive type.
        /// </summary>
        public static PrimitiveType Binary { get; } = new PrimitiveType((Identifier) "binary");

        /// <summary>
        /// Gets all predefined primitive types.
        /// </summary>
        public static IEnumerable<PrimitiveType> Defined { get; } = EnumerateAll();

        private static IEnumerable<PrimitiveType> EnumerateAll()
        {
            yield return Boolean;
            yield return Int32;
            yield return Int64;
            yield return Single;
            yield return Double;
            yield return Decimal;
            yield return String;
            yield return Guid;
            yield return DateTime;
            yield return DateTimeOffset;
            yield return TimeSpan;
            yield return Binary;
        }

        /// <inheritdoc />
        public override Identifier Name { get; }

        private PrimitiveType(Identifier name) => Name = name;
    }
}
