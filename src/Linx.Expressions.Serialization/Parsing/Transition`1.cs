namespace Linx.Expressions.Serialization.Parsing
{
    /// <summary>
    /// Transition on a range of characters to the successor state.
    /// </summary>
    public struct Transition<TState>
    {
        public char Range { get; }
        public TState Successor { get; }

        public Transition(char range, TState successor)
        {
            Range = range;
            Successor = successor;
        }

        public override string ToString() => $"{Range}->{Successor}";
    }
}
