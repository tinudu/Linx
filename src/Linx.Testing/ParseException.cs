using System;

namespace Linx.Testing;

/// <summary>
/// Exception thrown from parsing a pattern.
/// </summary>
public sealed class ParseException : Exception
{
    internal ParseException(string message, int position) : base($"Pos {position}: {message}") { }
}
