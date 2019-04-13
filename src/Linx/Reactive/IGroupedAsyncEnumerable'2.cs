namespace Linx.Reactive
{
    using System;

    /// <summary>
    /// A <see cref="IAsyncEnumerable{T}"/> with a key.
    /// </summary>
    public interface IGroupedAsyncEnumerable<out TKey, out TElement>
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        TKey Key { get; }

        /// <summary>
        /// Gets the elements.
        /// </summary>
        IAsyncEnumerable<TElement> Elements { get; }
    }

    /// <summary>
    /// Anonymous <see cref="IGroupedAsyncEnumerable{TKey, TElement}"/> implementation.
    /// </summary>
    public sealed class GroupedAsyncEnumerable<TKey, TElement> : IGroupedAsyncEnumerable<TKey, TElement>
    {
        /// <inheritdoc />
        public TKey Key { get; }

        /// <inheritdoc />
        public IAsyncEnumerable<TElement> Elements { get; }

        /// <summary>
        /// Initialize.
        /// </summary>
        public GroupedAsyncEnumerable(TKey key, IAsyncEnumerable<TElement> elements)
        {
            Key = key;
            Elements = elements ?? throw new ArgumentNullException(nameof(elements));
        }
    }
}
