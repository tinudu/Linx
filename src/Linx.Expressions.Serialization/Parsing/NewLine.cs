namespace Linx.Expressions.Serialization.Parsing
{
    public sealed class NewLine : IInputElement
    {
        public static NewLine Instance { get; } = new NewLine();
        private NewLine() { }

        public InputElementType InputElementType => InputElementType.NewLine;
    }
}
