namespace Linx
{
    /// <summary>
    /// A struct that provides a value.
    /// </summary>
    public struct ValueGetter<T>
    {
        /// <summary>
        /// Provides the value when <see cref="ValueGetter{T}.GetValue"/> is called.
        /// </summary>
        public interface IProvider
        {
            /// <summary>
            /// Get the value.
            /// </summary>
            T GetValue(short version);
        }

        private readonly IProvider _provider;
        private short _version;

        /// <summary>
        /// Initialize.
        /// </summary>
        public ValueGetter(IProvider provider, short version)
        {
            _provider = provider;
            _version = version;
        }

        /// <summary>
        /// Gets the value from the encapsulated provider.
        /// </summary>
        public T GetValue() => _provider is not null ? _provider.GetValue(_version) : default;
    }
}
