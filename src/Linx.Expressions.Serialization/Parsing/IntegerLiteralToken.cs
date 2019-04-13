namespace Linx.Expressions.Serialization.Parsing
{
    public sealed class IntegerLiteralToken : IToken
    {
        TokenType IToken.Type => TokenType.IntegerLiteral;
        public ulong Value { get; }


        public IntegerLiteralToken(ulong value)
        {
            Value = value;
        }
    }
}
