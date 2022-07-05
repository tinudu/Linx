namespace Linx.AsyncEnumerable;

/// <summary>
/// Provider for <see cref="ISubject{T}"/> instances.
/// </summary>
public interface IConnectable<T>
{
    /// <summary>
    /// Create a new subject.
    /// </summary>
    ISubject<T> CreateSubject();
}

