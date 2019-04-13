namespace Linx.Jsxn.TypeSystem
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An enumeration type.
    /// </summary>
    public sealed class EnumType : NonNullableType, INamedType
    {
        /// <summary>
        /// The name.
        /// </summary>
        public Identifier Name { get; }

        /// <summary>
        /// Enumeration values.
        /// </summary>
        public IReadOnlyList<Identifier> Values { get; }

        /// <summary>
        /// Initialize.
        /// </summary>
        public EnumType(Identifier name, IEnumerable<Identifier> values)
        {
            Name = name.Name != null ? name : throw new ArgumentNullException(nameof(name));

            if (values == null) throw new ArgumentNullException(nameof(values));
            var list = new List<Identifier>();
            var distinct = new HashSet<Identifier>();
            foreach (var m in values)
            {
                if (m.Name == null) throw new ArgumentException("Undefined value.", nameof(values));
                if (!distinct.Add(m)) throw new ArgumentException("Duplicate values.");
                list.Add(m);
            }
            Values = list.AsReadOnly();
        }

        /// <summary>
        /// <see cref="Name"/>.
        /// </summary>
        public override string ToString() => Name;
    }
}
