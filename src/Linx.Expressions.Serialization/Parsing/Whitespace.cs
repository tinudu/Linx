namespace Linx.Expressions.Serialization.Parsing
{
    public sealed class Whitespace : IInputElement
    {
        public static Whitespace Instance { get; } = new Whitespace();
        private Whitespace() { }

        public InputElementType InputElementType => InputElementType.Whitespace;
    }
}
