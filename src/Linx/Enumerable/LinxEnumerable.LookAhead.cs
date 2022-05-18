using System.Collections.Generic;

namespace Linx.Enumerable;

partial class LinxEnumerable
{
    /// <summary>
    /// Convenience method to create a <see cref="LookAhead{T}"/>.
    /// </summary>
    public static LookAhead<T> LookAhead<T>(this IEnumerable<T> source) => new(source);
}
