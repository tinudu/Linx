namespace Linx.AsyncEnumerable.Testing
{
    using System;

    /// <summary>
    /// Exception thrown from a marble error completion.
    /// </summary>
    public sealed class MarbleException : Exception
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static MarbleException Singleton { get; } = new MarbleException();

        private MarbleException() : base("Boom!") { }
    }
}
