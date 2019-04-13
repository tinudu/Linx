namespace Linx.Enumerable
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Comparer for enumerables.
    /// </summary>
    public sealed class SequenceComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        /// <summary>
        /// Uses <see cref="EqualityComparer{T}.Default"/> as the <see cref="ElementComparer"/>.
        /// </summary>
        public static SequenceComparer<T> Default { get; } = new SequenceComparer<T>();

        /// <summary>
        /// Gets the element comparer.
        /// </summary>
        public IEqualityComparer<T> ElementComparer { get; }

        private SequenceComparer() => ElementComparer = EqualityComparer<T>.Default;

        /// <summary>
        /// Initialize with the specified <see cref="IEqualityComparer{T}"/> to compare elements.
        /// </summary>
        /// <param name="elementComparer">Optional. Element comparer.</param>
        public SequenceComparer(IEqualityComparer<T> elementComparer) => ElementComparer = elementComparer ?? EqualityComparer<T>.Default;

        /// <inheritdoc />
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y) => x == null ? y == null : y != null && x.SequenceEqual(y, ElementComparer);

        /// <inheritdoc />
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        public int GetHashCode(IEnumerable<T> obj) => obj == null ? 0 : obj.Aggregate(new HashCode(), (a, c) => a + ElementComparer.GetHashCode());
    }
}
