namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a timestamped value.
    /// </summary>
    public struct Timestamped<T> : IEquatable<Timestamped<T>>
    {
        /// <summary>
        /// The timestamp.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// The value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initialize.
        /// </summary>
        public Timestamped(DateTimeOffset timestamp, T value)
        {
            Timestamp = timestamp;
            Value = value;
        }

        /// <inheritdoc />
        public bool Equals(Timestamped<T> other) => Timestamp == other.Timestamp && EqualityComparer<T>.Default.Equals(Value, other.Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Timestamped<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => new HashCode() + Timestamp.GetHashCode() + EqualityComparer<T>.Default.GetHashCode(Value);

        /// <inheritdoc />
        public override string ToString() => $"{Value}@{Timestamp}";
    }
}
