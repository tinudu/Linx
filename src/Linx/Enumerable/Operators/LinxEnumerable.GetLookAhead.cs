using System.Collections.Generic;

namespace Linx.Enumerable;

partial class LinxEnumerable
{
    /// <summary>
    /// Gets a <see cref="LookAhead{T}"/> from <paramref name="source"/>.
    /// </summary>
    public static LookAhead<T> GetLookAhead<T>(this IEnumerable<T> source) => new(source);
}
