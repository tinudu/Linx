namespace Linx.AsyncEnumerable.Testing
{
    using System;

    /// <summary>
    /// Exception thrown from the <see cref="MarbleParser"/>.
    /// </summary>
    public sealed class MarbleParseException : Exception
    {
        internal MarbleParseException(string message, int position) : base($"Pos {position}: {message}") { }
    }
}
