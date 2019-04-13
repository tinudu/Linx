namespace Linx.Jsxn
{
    using System;

    /// <summary>
    /// Wraps a string which is an identifier.
    /// </summary>
    public struct Identifier : IEquatable<Identifier>, IComparable<Identifier>
    {
        /// <summary>
        /// The wrapped string.
        /// </summary>
        public string Name { get; }

        private Identifier(string name)
        {
            // TODO: validate and normalize
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the <see cref="Name"/>.
        /// </summary>
        public static implicit operator string(Identifier x) => x.Name;

        /// <summary>
        /// Validate and normalize <paramref name="name"/> and wrap it into a <see cref="Identifier"/>.
        /// </summary>
        public static explicit operator Identifier(string name) => new Identifier(name);

        /// <summary>
        /// Equality.
        /// </summary>
        public static bool operator ==(Identifier x, Identifier y) => StringComparer.Ordinal.Equals(x.Name, y.Name);

        /// <summary>
        /// Inequality.
        /// </summary>
        public static bool operator !=(Identifier x, Identifier y) => !StringComparer.Ordinal.Equals(x.Name, y.Name);

        /// <summary>
        /// Equality.
        /// </summary>
        public bool Equals(Identifier other) => StringComparer.Ordinal.Equals(Name, other.Name);

        /// <summary>
        /// Equality.
        /// </summary>
        public override bool Equals(object obj) => obj is Identifier && StringComparer.Ordinal.Equals(Name, ((Identifier)obj).Name);

        /// <summary>
        /// Hash code.
        /// </summary>
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Name);

        /// <summary>
        /// Compare names.
        /// </summary>
        public int CompareTo(Identifier other) => string.Compare(Name, other.Name, StringComparison.Ordinal);

        /// <summary>
        /// <see cref="Name"/>.
        /// </summary>
        public override string ToString() => Name ?? string.Empty;
    }
}
