﻿namespace Linx.Enumerable
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
        public static SequenceComparer<T> Default { get; } = new SequenceComparer<T>(EqualityComparer<T>.Default);

        /// <summary>
        /// Gets an instance.
        /// </summary>
        public static SequenceComparer<T> GetComparer(IEqualityComparer<T> elementComparer) => elementComparer == null || elementComparer == EqualityComparer<T>.Default ? Default : new SequenceComparer<T>(elementComparer);

        /// <summary>
        /// Gets the element comparer.
        /// </summary>
        public IEqualityComparer<T> ElementComparer { get; }

        private SequenceComparer(IEqualityComparer<T> elementComparer) => ElementComparer = elementComparer;

        /// <inheritdoc />
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y) => x == null ? y == null : y != null && x.SequenceEqual(y, ElementComparer);

        /// <inheritdoc />
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        public int GetHashCode(IEnumerable<T> obj) => obj == null ? 0 : obj.Aggregate(new HashCode(), (a, c) => a + ElementComparer.GetHashCode());
    }
}
