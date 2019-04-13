namespace Linx.Expressions.Serialization
{
    using System;
    using System.Globalization;
    using System.Text;
    using Parsing;

    public sealed class Identifier : IEquatable<Identifier>, IComparable<Identifier>
    {
        public static Identifier Create(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            using (var ucs = UnicodeChar.Decode(name).GetEnumerator())
            {
                if (!ucs.MoveNext()) throw new ArgumentOutOfRangeException(nameof(name), "Invalid identifier.");

                var current = ucs.Current;
                var isSupplementary = current.IsInSupplementaryPlane;
                if (current != '_')
                {
                    var ucc = isSupplementary ? CharUnicodeInfo.GetUnicodeCategory(name, 0) : CharUnicodeInfo.GetUnicodeCategory((char)current);
                    switch (ucc)
                    {
                        case UnicodeCategory.UppercaseLetter:
                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.TitlecaseLetter:
                        case UnicodeCategory.ModifierLetter:
                        case UnicodeCategory.OtherLetter:
                        case UnicodeCategory.LetterNumber:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(name), "Invalid identifier.");
                    }
                }
                var ixUtf16 = isSupplementary ? 2 : 1;

                StringBuilder sb = null;
                while (ucs.MoveNext())
                {
                    current = ucs.Current;
                    isSupplementary = current.IsInSupplementaryPlane;
                    var ucc = isSupplementary ? CharUnicodeInfo.GetUnicodeCategory(name, ixUtf16) : CharUnicodeInfo.GetUnicodeCategory((char)current);
                    switch (ucc)
                    {
                        case UnicodeCategory.UppercaseLetter:
                        case UnicodeCategory.LowercaseLetter:
                        case UnicodeCategory.TitlecaseLetter:
                        case UnicodeCategory.ModifierLetter:
                        case UnicodeCategory.OtherLetter:
                        case UnicodeCategory.LetterNumber:
                        case UnicodeCategory.NonSpacingMark:
                        case UnicodeCategory.SpacingCombiningMark:
                        case UnicodeCategory.DecimalDigitNumber:
                        case UnicodeCategory.ConnectorPunctuation:
                            if (sb != null) current.AppendTo(sb);
                            break;
                        case UnicodeCategory.Format:
                            if (sb == null) sb = new StringBuilder(name, 0, ixUtf16, name.Length);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(name), "Invalid identifier.");
                    }
                    ixUtf16 += isSupplementary ? 2 : 1;
                }
                return new Identifier(sb == null ? name : sb.ToString());
            }
        }

        public string Name { get; }

        private Identifier(string name) => Name = name;

        public static bool operator ==(Identifier x, Identifier y) => x?.Name == y?.Name;
        public static bool operator !=(Identifier x, Identifier y) => x?.Name != y?.Name;

        public bool Equals(Identifier other) => other != null && Name == other.Name;
        public int CompareTo(Identifier other) => String.Compare(Name, other?.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as Identifier);
        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;
    }
}

