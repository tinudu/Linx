namespace Linx.Expressions.Serialization.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Enumerable;

    public struct UnicodeChar : IEquatable<UnicodeChar>, IComparable<UnicodeChar>
    {
        public static IEnumerable<UnicodeChar> Decode(IEnumerable<char> chars)
        {
            if (chars == null) throw new ArgumentNullException(nameof(chars));

            using (var la = new LookAhead<char>(chars))
                while (la.HasNext)
                {
                    var current = la.Next;
                    la.MoveNext();
                    if ((current & 0xFC00) == 0xD800 && la.HasNext && (la.Next & 0xFC00) == 0xDC00) // surrogate pair
                    {
                        yield return new UnicodeChar(0x10000 + (((current & 0x3FF) << 10) | (la.Next & 0x3FF)));
                        la.MoveNext();
                    }
                    else
                        yield return new UnicodeChar(current);
                }
        }

        public int CodePoint { get; }
        public bool IsInSupplementaryPlane => CodePoint > char.MaxValue;

        public UnicodeChar(int codePoint) => CodePoint = codePoint <= 0x10FFFF ? codePoint : throw new ArgumentOutOfRangeException(nameof(codePoint));

        public static implicit operator UnicodeChar(char ch) => new UnicodeChar(ch);
        public static explicit operator char(UnicodeChar uch) => checked((char)uch.CodePoint);
        public static bool operator ==(UnicodeChar x, UnicodeChar y) => x.CodePoint == y.CodePoint;
        public static bool operator !=(UnicodeChar x, UnicodeChar y) => x.CodePoint != y.CodePoint;
        public static bool operator <(UnicodeChar x, UnicodeChar y) => x.CodePoint < y.CodePoint;
        public static bool operator <=(UnicodeChar x, UnicodeChar y) => x.CodePoint <= y.CodePoint;
        public static bool operator >(UnicodeChar x, UnicodeChar y) => x.CodePoint > y.CodePoint;
        public static bool operator >=(UnicodeChar x, UnicodeChar y) => x.CodePoint >= y.CodePoint;

        public bool Equals(UnicodeChar other) => CodePoint == other.CodePoint;
        public int CompareTo(UnicodeChar other) => CodePoint.CompareTo(other.CodePoint);

        public override bool Equals(object obj) => obj is UnicodeChar c && Equals(c);
        public override int GetHashCode() => CodePoint;

        public UnicodeCategory GetUnicodeCategory() => CodePoint <= char.MaxValue ? CharUnicodeInfo.GetUnicodeCategory((char)CodePoint) : CharUnicodeInfo.GetUnicodeCategory(ToString(), 0);

        public void AppendTo(StringBuilder sb)
        {
            if (CodePoint <= char.MaxValue)
                sb.Append((char)CodePoint);
            else
            {
                var cp = CodePoint - 0x10000;
                sb.Append((char)(0xD800 | (cp >> 10)));
                sb.Append((char)(0xDC00 | (cp & 0x3FF)));
            }
        }

        public override string ToString()
        {
            if (CodePoint <= char.MaxValue) return ((char)CodePoint).ToString();
            var cp = CodePoint - 0x10000;
            return new string(new[] { (char)(0xD800 | (cp >> 10)), (char)(0xDC00 | (cp & 0x3FF)) });
        }
    }
}
