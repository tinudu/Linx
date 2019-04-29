namespace Linx.Reactive
{
    /// <summary>
    /// A <see cref="IAsyncEnumerableObs{T}"/> with a key.
    /// </summary>
    public interface IGroupedAsyncEnumerable<out TKey, out TElement> : IAsyncEnumerableObs<TElement>
    {
        /// <summary>
        /// Gets the key.
        /// </summary>
        TKey Key { get; }
    }
}
