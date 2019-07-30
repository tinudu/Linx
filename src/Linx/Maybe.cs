namespace Linx
{
    using System;

    /// <summary>
    /// Static <see cref="Maybe{T}"/> methods.
    /// </summary>
    public static class Maybe
    {
        /// <summary>
        /// Convert <paramref name="value"/> to a <see cref="Maybe{T}"/> that <see cref="Maybe{T}.HasValue"/>.
        /// </summary>
        public static Maybe<T> AsIs<T>(T value) => new Maybe<T>(value);

        /// <summary>
        /// Convert <paramref name="value"/> to a <see cref="Maybe{T}"/> that <see cref="Maybe{T}.HasValue"/> if <paramref name="value"/> is not null.
        /// </summary>
        public static Maybe<T> IfNotNull<T>(T value) => value != null ? new Maybe<T>(value) : default;

        /// <summary>
        /// Try to parse the specified <see cref="string"/> using the specified <see cref="TryParseDelegate{T}"/> to a <see cref="Maybe{T}"/>.
        /// </summary>
        public static Maybe<T> TryParse<T>(string s, TryParseDelegate<T> tryParse) => tryParse(s, out var value) ? new Maybe<T>(value) : default;
    }

    /// <summary>
    /// Represents a value or no value.
    /// </summary>
    /// <remarks>
    /// If <see cref="HasValue"/> is true, <see cref="Value"/> is whatever value the <see cref="Maybe{T}"/> was created with, including nulls.
    /// </remarks>
    public struct Maybe<T>
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance to have the specified value.
        /// </summary>
        public Maybe(T value)
        {
            HasValue = true;
            _value = value;
        }

        /// <summary>
        /// Gets whether there is a <see cref="Value"/>.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <exception cref="InvalidOperationException">No value.</exception>
        public T Value => HasValue ? _value : throw new InvalidOperationException("No value.");

        /// <summary>
        /// Gets the <see cref="Value"/> if <see cref="HasValue"/>, or the default value of <typeparamref name="T"/>.
        /// </summary>
        public T GetValueOrDefault() => _value;

        /// <summary>
        /// Gets the value if <see cref="HasValue"/>, or the specified <paramref name="defaultValue"/>.
        /// </summary>
        public T GetValueOrDefault(T defaultValue) => HasValue ? _value : defaultValue;

        /// <summary>
        /// Try to get the value.
        /// </summary>
        /// <param name="value">The <see cref="Value"/> if <see cref="HasValue"/>, or the default value of <typeparamref name="T"/></param>
        /// <returns><see cref="HasValue"/>.</returns>
        public bool TryGetValue(out T value)
        {
            value = _value;
            return HasValue;
        }

        /// <summary>
        /// Gets the string representation of <see cref="Value"/> if assigned and not null, or an empty string.
        /// </summary>
        public override string ToString() => HasValue ? _value?.ToString() ?? string.Empty : string.Empty;

        /// <summary>
        /// Implicit conversion from <typeparamref name="T"/> to <see cref="Maybe{T}"/>.
        /// </summary>
        public static implicit operator Maybe<T>(T value) => new Maybe<T>(value);

        /// <summary>
        /// Explicit conversion from <see cref="Maybe{T}"/> to <typeparamref name="T"/>.
        /// </summary>
        public static explicit operator T(Maybe<T> maybe) => maybe.Value;
    }
}
