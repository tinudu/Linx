namespace Linx.Collections
{
    using System.Collections.Generic;

    /// <summary>
    /// Read only list of <typeparamref name="TItem"/> with a key embedded in the item.
    /// </summary>
    public interface IReadOnlyKeyedCollection<in TKey, out TItem> : IReadOnlyList<TItem>
    {
        /// <summary>
        /// Gets whether there is an item with the specified key.
        /// </summary>
        bool Contains(TKey key);

        /// <summary>
        /// Gets an item by its key.
        /// </summary>
        TItem this[TKey key] { get; }   
    }
}
