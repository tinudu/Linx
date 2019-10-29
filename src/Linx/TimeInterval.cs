namespace Linx
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Static <see cref="TimeInterval{T}"/> methods.
    /// </summary>
    public static class TimeInterval
    {
        /// <summary>
        /// <see cref="TimeInterval{T}"/> factory method.
        /// </summary>
        public static TimeInterval<T> Create<T>(T value, TimeSpan interval) => new TimeInterval<T>(interval, value);

        /// <summary>
        /// Get a <see cref="IEqualityComparer{T}"/> for <see cref="TimeInterval{T}"/> using the specified comparer for <typeparamref name="T"/>.
        /// </summary>
        public static IEqualityComparer<TimeInterval<T>> GetEqualityComparer<T>(IEqualityComparer<T> valueComparer) => TimeIntervalEqualityComparer<T>.Create(valueComparer);

        private sealed class TimeIntervalEqualityComparer<T> : IEqualityComparer<TimeInterval<T>>
        {
            public static IEqualityComparer<TimeInterval<T>> Create(IEqualityComparer<T> valueComparer) => valueComparer == null || valueComparer == EqualityComparer<T>.Default ? (IEqualityComparer<TimeInterval<T>>)EqualityComparer<TimeInterval<T>>.Default : new TimeIntervalEqualityComparer<T>(valueComparer);
            private readonly IEqualityComparer<T> _valueComparer;
            private TimeIntervalEqualityComparer(IEqualityComparer<T> valueComparer) => _valueComparer = valueComparer;
            public bool Equals(TimeInterval<T> x, TimeInterval<T> y) => x.Interval == y.Interval && _valueComparer.Equals(x.Value, y.Value);
            public int GetHashCode(TimeInterval<T> obj) => HashCode.Combine(obj.Interval, _valueComparer.GetHashCode(obj.Value));
        }
    }

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
        public override int GetHashCode() => HashCode.Combine(Interval, EqualityComparer<T>.Default.GetHashCode(Value));

        /// <inheritdoc />
        public override string ToString() => $"{Value}@{Interval}";
    }
}
