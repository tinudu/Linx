namespace Linx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Static <see cref="Boxed{T}"/> methods.
    /// </summary>
    [DebuggerNonUserCode]
    public static class Boxed
    {
        /// <summary>
        /// Create a <see cref="Boxed{T}"/> from the specified value.
        /// </summary>
        public static Boxed<T> Create<T>(T value) => new(value);

        /// <summary>
        /// Get a <see cref="IEqualityComparer{T}"/> for <see cref="Boxed{T}"/> using the specified comparer for <typeparamref name="T"/>.
        /// </summary>
        public static IEqualityComparer<Boxed<T>> GetEqualityComparer<T>(IEqualityComparer<T> valueComparer) => BoxedEqualityComparer<T>.Create(valueComparer);

        private sealed class BoxedEqualityComparer<T>:IEqualityComparer<Boxed<T>>
        {
            public static IEqualityComparer<Boxed<T>> Create(IEqualityComparer<T> valueComparer) => 
                valueComparer == null || valueComparer == EqualityComparer<T>.Default 
                    ? (IEqualityComparer<Boxed<T>>) EqualityComparer<Boxed<T>>.Default 
                    : new BoxedEqualityComparer<T>(valueComparer);

            private readonly IEqualityComparer<T> _valueComparer;
            private BoxedEqualityComparer(IEqualityComparer<T> valueComparer) => _valueComparer = valueComparer;
            public bool Equals(Boxed<T> x, Boxed<T> y) => _valueComparer.Equals(x.Value, y.Value);
            public int GetHashCode(Boxed<T> obj) => _valueComparer.GetHashCode(obj.Value);
        }
    }

    /// <summary>
    /// A value of type <typeparamref name="T"/>, wrapped into a struct.
    /// </summary>
    /// <remarks>Useful in places where nulls are not allowed, like dictionary keys.</remarks>
    [DebuggerNonUserCode]
    public struct Boxed<T> : IEquatable<Boxed<T>>
    {
        /// <summary>
        /// Gets the boxed value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initialize with a value.
        /// </summary>
        public Boxed(T value) => Value = value;

        /// <inheritdoc />
        public bool Equals(Boxed<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Boxed<T> b && Equals(b);

        /// <inheritdoc />
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);

        /// <inheritdoc />
        public override string ToString() => Value?.ToString() ?? string.Empty;

        /// <summary>
        /// Implicit <see cref="Boxed{T}"/> to <typeparamref name="T"/> conversion.
        /// </summary>
        public static implicit operator Boxed<T>(T value) => new(value);

        /// <summary>
        /// Implicit <typeparamref name="T"/> to <see cref="Boxed{T}"/> conversion.
        /// </summary>
        public static implicit operator T(Boxed<T> boxed) => boxed.Value;

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(Boxed<T> x, Boxed<T> y) => EqualityComparer<T>.Default.Equals(x.Value, y.Value);

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(Boxed<T> x, Boxed<T> y) => !EqualityComparer<T>.Default.Equals(x.Value, y.Value);
    }
}
