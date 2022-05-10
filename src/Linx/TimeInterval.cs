namespace Linx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Static <see cref="TimeInterval{T}"/> methods.
    /// </summary>
    [DebuggerNonUserCode]
    public static class TimeInterval
    {
        /// <summary>
        /// <see cref="TimeInterval{T}"/> factory method.
        /// </summary>
        public static TimeInterval<T> Create<T>(TimeSpan interval, T value) => new(interval, value);

        /// <summary>
        /// Get a <see cref="IEqualityComparer{T}"/> for <see cref="TimeInterval{T}"/> using the specified comparer for <typeparamref name="T"/>.
        /// </summary>
        public static IEqualityComparer<TimeInterval<T>> GetEqualityComparer<T>(IEqualityComparer<T>? valueComparer = null) =>
            valueComparer is null || ReferenceEquals(valueComparer, EqualityComparer<T>.Default) ?
            EqualityComparer<TimeInterval<T>>.Default :
            new TimeIntervalEqualityComparer<T>(valueComparer);

        private sealed class TimeIntervalEqualityComparer<T> : IEqualityComparer<TimeInterval<T>>
        {
            private readonly IEqualityComparer<T> _valueComparer;

            public TimeIntervalEqualityComparer(IEqualityComparer<T> valueComparer) => _valueComparer = valueComparer;

            public bool Equals(TimeInterval<T> x, TimeInterval<T> y) =>
                x.Interval == y.Interval &&
                _valueComparer.Equals(x.Value, y.Value);

            public int GetHashCode(TimeInterval<T> obj)
            {
                var hc = new HashCode();
                hc.Add(obj.Interval);
                hc.Add(obj.Value, _valueComparer);
                return hc.ToHashCode();
            }
        }
    }

    /// <summary>
    /// Represents a time interval value.
    /// </summary>
    [DebuggerNonUserCode]
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
        public bool Equals(TimeInterval<T> other) =>
            Interval == other.Interval &&
            EqualityComparer<T>.Default.Equals(Value, other.Value);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is TimeInterval<T> other && Equals(other);

        /// <summary>
        /// Equality.
        /// </summary>
        public static bool operator ==(TimeInterval<T> left, TimeInterval<T> right) => left.Equals(right);

        /// <summary>
        /// Inequality.
        /// </summary>
        public static bool operator !=(TimeInterval<T> left, TimeInterval<T> right) => !left.Equals(right);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Interval, Value);

        /// <inheritdoc />
        public override string ToString() => $"{Value}@{Interval}";
    }
}
