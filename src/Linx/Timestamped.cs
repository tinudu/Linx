namespace Linx
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Static <see cref="Timestamped{T}"/> methods.
    /// </summary>
    public static class Timestamped
    {
        /// <summary>
        /// Timestamp the specified value with the specified timestamp.
        /// </summary>
        public static Timestamped<T> Create<T>(DateTimeOffset timestamp, T value ) => new Timestamped<T>(timestamp, value);
    }

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
