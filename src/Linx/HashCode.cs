namespace Linx
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Compute a hash code.
    /// </summary>
    public struct HashCode
    {
        // magic numbers
        private const int _seed = 0x20e699b;
        private const int _factor = unchecked((int)0xa5555529);

        private readonly int? _value;

        private HashCode(int value) => _value = value;

        /// <summary>
        /// Convert to the actual hash code based on what was added so far.
        /// </summary>
        public static implicit operator int(HashCode hc) => hc._value ?? 0;

        /// <summary>
        /// Add a hash code to the state.
        /// </summary>
        /// <returns>An updated <see cref="HashCode"/>.</returns>
        public static HashCode operator +(HashCode hc, int other) => new HashCode(unchecked((hc._value * _factor ?? _seed) + other));

        /// <summary>
        /// Add a sequence of hash code to the state.
        /// </summary>
        /// <returns>An updated <see cref="HashCode"/>.</returns>
        public static HashCode operator +(HashCode hc, IEnumerable<int> others) => others.Aggregate(hc, (a, c) => a + c);
    }
}
