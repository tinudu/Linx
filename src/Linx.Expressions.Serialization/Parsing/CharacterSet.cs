namespace Linx.Expressions.Serialization.Parsing
{
    using System.Globalization;

    public static class CharacterSet
    {
        public static bool IsInputCharacter(this char ch) => !ch.IsNewLineCharacter();
        public static bool IsNewLineCharacter(this char ch) => ch == '\u000D' || ch == '\u000A' || ch == '\u0085' || ch == '\u2028' || ch == '\u2029';
        public static bool IsWhitespace(this char ch) => ch == '\u0009' || ch == '\u000B' || ch == '\u000C' || CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.SpaceSeparator;
    }
}
