﻿namespace Linx.Reactive
{
    /// <summary>
    /// A <see cref="IAsyncEnumerable{T}"/> with a key.
    /// </summary>
    public interface IGroupedAsyncEnumerable<out TKey, out TElement> : IAsyncEnumerable<TElement>
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        TKey Key { get; }
    }
}
