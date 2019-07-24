namespace Linx
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A value of type <typeparamref name="T"/>, wrapped into a struct.
    /// </summary>
    /// <remarks>Compares null values as equal, therefore useful for hashing null references and nullables.</remarks>
    public struct Wrapped<T> : IEquatable<Wrapped<T>>
    {
        private const int _nullHash = 0xCAFE;

        /// <summary>
        /// Gets a <see cref="IEqualityComparer{T}"/> that uses the <see cref="EqualityComparer{T}.Default"/>.
        /// </summary>
        public static IEqualityComparer<Wrapped<T>> DefaultComparer { get; } = new EqualityComparer(EqualityComparer<T>.Default);

        /// <summary>
        /// Gets a <see cref="IEqualityComparer{T}"/> that uses the specified <paramref name="comparer"/>.
        /// </summary>
        public static IEqualityComparer<Wrapped<T>> GetComparer(IEqualityComparer<T> comparer)
            => comparer == null || ReferenceEquals(comparer, EqualityComparer<T>.Default) ? DefaultComparer : new EqualityComparer(comparer);

        /// <summary>
        /// Gets the wrapped value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initialize with a value.
        /// </summary>
        public Wrapped(T value) => Value = value;

        /// <inheritdoc />
        public bool Equals(Wrapped<T> other) => Value == null ? other.Value == null : other.Value != null && EqualityComparer<T>.Default.Equals(Value, other.Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Wrapped<T> w && Equals(w);

        /// <inheritdoc />
        public override int GetHashCode() => Value != null ? EqualityComparer<T>.Default.GetHashCode(Value) : _nullHash;

        /// <inheritdoc />
        public override string ToString() => Value?.ToString() ?? string.Empty;

        private sealed class EqualityComparer : IEqualityComparer<Wrapped<T>>
        {
            private readonly IEqualityComparer<T> _comparer;
            public EqualityComparer(IEqualityComparer<T> comparer) => _comparer = comparer;
            public bool Equals(Wrapped<T> x, Wrapped<T> y) => x.Value == null ? y.Value == null : y.Value != null && _comparer.Equals(x.Value, y.Value);
            public int GetHashCode(Wrapped<T> obj) => obj.Value != null ? _comparer.GetHashCode(obj.Value) : _nullHash;
        }

        /// <summary>
        /// Implicit <see cref="Wrapped{T}"/> to <typeparamref name="T"/> conversion.
        /// </summary>
        public static implicit operator Wrapped<T>(T value) => new Wrapped<T>();

        /// <summary>
        /// Implicit <typeparamref name="T"/> to <see cref="Wrapped{T}"/> conversion.
        /// </summary>
        public static implicit operator T(Wrapped<T> wrapped) => wrapped.Value;
    }
}
