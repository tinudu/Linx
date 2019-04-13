namespace Linx.Expressions.Serialization.Parsing
{
    using System;

    public sealed class IdentifierToken : IToken
    {
        TokenType IToken.Type => TokenType.Identifier;
        public string Identifier { get; }

        public IdentifierToken(string identifier) => Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));

        public override string ToString() => Identifier;
    }
}
