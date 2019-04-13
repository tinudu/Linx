namespace Linx.Jsxn.Schema
{
    using System.Collections.Generic;

    /// <summary>
    /// An enumeration type.
    /// </summary>
    public sealed class EnumType : NamedType, JsxnSchemaBuilder.ISchemaType
    {
        /// <inheritdoc />
        public override Identifier Name { get; }

        /// <summary>
        /// Gets whether this enum is a set of values rather than a single value.
        /// </summary>
        public bool Flags { get; set; }

        Identifier JsxnSchemaBuilder.ISchemaType.Name => Name;

        /// <summary>
        /// Enumeration values.
        /// </summary>
        public IReadOnlyCollection<Identifier> Values { get; }

        internal EnumType(Identifier name, bool flags, IReadOnlyCollection<Identifier> values)
        {
            Name = name;
            Flags = flags;
            Values = values;
        }
    }
}
