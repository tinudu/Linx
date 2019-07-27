namespace Linx
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Hash code builder.
    /// </summary>
    /// <remarks>All methods are null tolerant.</remarks>
    [DebuggerNonUserCode]
    public struct HashCode : IEquatable<HashCode>
    {
        // magic numbers
        private const int _seed = 0x20e699b;
        private const int _factor = unchecked((int)0xa5555529);

        private readonly int? _hash;
        private HashCode(int hash) => _hash = hash;

        /// <summary>
        /// Get a <see cref="HashCode"/> based on the current hash and a hash calculated from the specified <paramref name="obj"/>.
        /// </summary>
        public HashCode Hash<T>(T obj)
        {
            unchecked
            {
                var seed = _hash * _factor ?? _seed;
                return new HashCode(seed + obj?.GetHashCode() ?? typeof(T).GetHashCode());
            }
        }

        /// <summary>
        /// Get a <see cref="HashCode"/> based on the current hash and a hash calculated from the specified <paramref name="obj"/>.
        /// </summary>
        public HashCode Hash<T>(T obj, IEqualityComparer<T> comparer)
        {
            unchecked
            {
                var seed = _hash * _factor ?? _seed;
                var hash = obj == null ? typeof(T).GetHashCode() : comparer?.GetHashCode(obj) ?? obj.GetHashCode();
                return new HashCode(seed + hash);
            }
        }

        /// <summary>
        /// Get a <see cref="HashCode"/> based on the current hash, the hash codes of indidual sequence elements and the length of the sequence.
        /// </summary>
        public HashCode HashMany<T>(IEnumerable<T> objs, IEqualityComparer<T> elementComparer = null)
        {
            unchecked
            {
                var seed = _hash * _factor ?? _seed;
                if (objs == null) return new HashCode(seed + typeof(IEnumerable<T>).GetHashCode());
                if (elementComparer == null) elementComparer = EqualityComparer<T>.Default;
                var count = 0;
                foreach (var obj in objs)
                {
                    var hash = obj == null ? typeof(T).GetHashCode() : elementComparer.GetHashCode(obj);
                    seed = (seed + hash) * _factor;
                    count++;
                }
                return new HashCode(seed + count);
            }
        }

        /// <inheritdoc />
        public bool Equals(HashCode other) => GetHashCode() == other.GetHashCode();

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is HashCode h && GetHashCode() == h.GetHashCode();

        /// <inheritdoc />
        public override int GetHashCode() => _hash ?? 0;

        /// <summary>
        /// Convert to the calculated hash code.
        /// </summary>
        public static implicit operator int(HashCode hashCode) => hashCode._hash ?? 0;

        /// <inheritdoc />
        public override string ToString() => GetHashCode().ToString();
    }
}
