namespace Linx;

/// <summary>
/// Signature of a TryParse method.
/// </summary>
public delegate bool TryParseDelegate<T>(string s, out T value);
