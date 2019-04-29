namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a time interval value.
    /// </summary>
    public struct TimeInterval<T> : IEquatable<TimeInterval<T>>
    {
        /// <summary>
        /// The interval.
        /// </summary>
        public TimeSpan Interval { get; }

        /// <summary>
        /// The value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initialize.
        /// </summary>
        public TimeInterval(TimeSpan interval, T value)
        {
            Interval = interval;
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(TimeInterval<T> other) => Interval == other.Interval && EqualityComparer<T>.Default.Equals(Value, other.Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is TimeInterval<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => new HashCode() + Interval.GetHashCode() + EqualityComparer<T>.Default.GetHashCode(Value);

        /// <inheritdoc />
        public override string ToString() => $"{Value}@{Interval}";
    }
}
