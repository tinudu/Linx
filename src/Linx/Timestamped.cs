namespace Linx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Static <see cref="Timestamped{T}"/> methods.
    /// </summary>
    [DebuggerNonUserCode]
    public static class Timestamped
    {
        /// <summary>
        /// Timestamp the specified value with the specified timestamp.
        /// </summary>
        public static Timestamped<T> Create<T>(DateTimeOffset timestamp, T value) => new(timestamp, value);

        /// <summary>
        /// Get a <see cref="IEqualityComparer{T}"/> for <see cref="TimeInterval{T}"/> using the specified comparer for <typeparamref name="T"/>.
        /// </summary>
        public static IEqualityComparer<Timestamped<T>> GetEqualityComparer<T>(IEqualityComparer<T> valueComparer) => TimestampedEqualityComparer<T>.Create(valueComparer);

        private sealed class TimestampedEqualityComparer<T> : IEqualityComparer<Timestamped<T>>
        {
            public static IEqualityComparer<Timestamped<T>> Create(IEqualityComparer<T> valueComparer)
                => valueComparer == null || valueComparer == EqualityComparer<Timestamped<T>>.Default
                    ? (IEqualityComparer<Timestamped<T>>)EqualityComparer<Timestamped<T>>.Default
                    : new TimestampedEqualityComparer<T>(valueComparer);

            private readonly IEqualityComparer<T> _valueComparer;
            private TimestampedEqualityComparer(IEqualityComparer<T> valueComparer) => _valueComparer = valueComparer;
            public bool Equals(Timestamped<T> x, Timestamped<T> y) => x.Timestamp == y.Timestamp && _valueComparer.Equals(x.Value, y.Value);
            public int GetHashCode(Timestamped<T> obj) => HashCode.Combine(obj.Timestamp, _valueComparer.GetHashCode(obj.Value));
        }
    }

    /// <summary>
    /// Represents a timestamped value.
    /// </summary>
    [DebuggerNonUserCode]
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

        /// <summary>
        /// Equality.
        /// </summary>
        public static bool operator ==(Timestamped<T> left, Timestamped<T> right) => left.Equals(right);

        /// <summary>
        /// Inequality.
        /// </summary>
        public static bool operator !=(Timestamped<T> left, Timestamped<T> right) => !left.Equals(right);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Timestamp, EqualityComparer<T>.Default.GetHashCode(Value));

        /// <inheritdoc />
        public override string ToString() => $"{Value}@{Timestamp}";
    }
}
