﻿namespace Linx
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
        public override int GetHashCode() => new Hasher().Hash(Interval).Hash(Value);

        /// <inheritdoc />
        public override string ToString() => $"{Value}@{Interval}";
    }
}
