namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;

    /// <summary>
    /// A <see cref="IAsyncEnumerable{T}"/> with a key.
    /// </summary>
    public interface IAsyncGrouping<out TKey, out TElement> : IAsyncEnumerable<TElement>
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        TKey Key { get; }
    }
}
