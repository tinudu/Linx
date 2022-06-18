using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

/// <summary>
/// Defines a sort order on a <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public interface IOrderedAsyncEnumerable<T>
{
    /// <summary>
    /// Gets the source sequence.
    /// </summary>
    IAsyncEnumerable<T> Source { get; }

    /// <summary>
    /// Defines the order.
    /// </summary>
    Comparison<T> Comparison { get; }
}
