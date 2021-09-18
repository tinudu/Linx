namespace Linx.AsyncEnumerable
{
    /// <summary>
    /// Encapsulates the result of a lossy operation.
    /// </summary>
    public struct Lossy<T>
    {
        /// <summary>
        /// Gets the dequeued value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the number of ignored items before (MostRecent) or after (LeastRecent) the value was dequeued.
        /// </summary>
        public int IgnoredCount { get; }

        internal Lossy(T value, int ignoredCount)
        {
            Value = value;
            IgnoredCount = ignoredCount;
        }
    }
}
