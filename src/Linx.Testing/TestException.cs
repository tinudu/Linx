namespace Linx.Testing
{
    using System;

    /// <summary>
    /// Default <see cref="Exception"/> from a test sequence.
    /// </summary>
    public sealed class TestException : Exception
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static TestException Singleton { get; } = new TestException();

        private TestException() : base("Boom!") { }
    }
}
