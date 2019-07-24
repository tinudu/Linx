namespace Linx
{
    using System;

    /// <summary>
    /// Represents a value that is either assigned or not.
    /// </summary>
    public struct Maybe<T>
    {
        private readonly T _value;

        /// <summary>
        /// Initializes a new instance to have the specified value (not checked for null).
        /// </summary>
        public Maybe(T value)
        {
            HasValue = true;
            _value = value;
        }

        /// <summary>
        /// Gets whether the <see cref="Value"/> was assigned.
        /// </summary>
        public bool HasValue { get; }

        /// <summary>
        /// Gets the assigned value.
        /// </summary>
        /// <exception cref="InvalidOperationException">No value.</exception>
        public T Value => HasValue ? _value : throw new InvalidOperationException("No value.");

        /// <summary>
        /// Gets the assigned value if it's assigned, or the default value of <typeparamref name="T"/>.
        /// </summary>
        public T GetValueOrDefault() => _value;

        /// <summary>
        /// Gets the string representation of <see cref="Value"/> if assigned and not null, or an empty string.
        /// </summary>
        public override string ToString() => HasValue ? _value?.ToString() ?? string.Empty : string.Empty;

        /// <summary>
        /// Implicit conversion from <typeparamref name="T"/> to <see cref="Maybe{T}"/>.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Maybe<T>(T value) => new Maybe<T>(value);

        /// <summary>
        /// Explicit conversion from <see cref="Maybe{T}"/> to <typeparamref name="T"/>.
        /// </summary>
        public static explicit operator T(Maybe<T> maybe) => maybe.Value;
    }
}
