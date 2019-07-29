namespace Linx.Testing
{
    using System;

    /// <summary>
    /// Exception thrown from the <see cref="Marble"/>.
    /// </summary>
    public sealed class MarbleParseException : Exception
    {
        internal MarbleParseException(string message, int position) : base($"Pos {position}: {message}") { }
    }
}
