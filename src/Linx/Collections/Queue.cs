namespace Linx.Collections
{
    using System;

    /// <summary>
    /// Provides a set of <see cref="IQueue{T}"/> factory methods.
    /// </summary>
    public static partial class Queue
    {
        /// <summary>
        /// Gets a <see cref="IQueue{T}"/> that is always empty and is never full.
        /// </summary>
        public static IQueue<T> Empty<T>() => EmptyQueue<T>.Instance;

        /// <summary>
        /// Gets a <see cref="IQueue{T}"/> that is never full and stores just the most recent item.
        /// </summary>
        public static IQueue<T> Latest<T>() => new LatestOneQueue<T>();

        /// <summary>
        /// Gets a <see cref="IQueue{T}"/> that is never full and stores the <paramref name="maxCount"/> most recent item.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
        public static IQueue<T> Latest<T>(int maxCount)
        {
            if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount));
            switch (maxCount)
            {
                case 0: return EmptyQueue<T>.Instance;
                case 1: return new LatestOneQueue<T>();
                default: return new LatestMaxCountQueue<T>(maxCount);
            }
        }
    }
}
