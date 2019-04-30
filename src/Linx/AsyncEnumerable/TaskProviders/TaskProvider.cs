namespace Linx.AsyncEnumerable.TaskProviders
{
    /// <summary>
    /// Task provider factory methods.
    /// </summary>
    public static class TaskProvider
    {
        /// <summary>
        /// Create a <see cref="ManualResetProvider"/>.
        /// </summary>
        public static ManualResetProvider ManualReset() => new ManualResetProvider(default);

        /// <summary>
        /// Create a <see cref="ManualResetProvider{T}"/>.
        /// </summary>
        public static ManualResetProvider<T> ManualReset<T>() => new ManualResetProvider<T>(default);

        /// <summary>
        /// Create a <see cref="ManualResetConfiguredProvider"/>.
        /// </summary>
        public static ManualResetConfiguredProvider ManualReset(bool continueOnCapturedContext) => new ManualResetConfiguredProvider(continueOnCapturedContext);

        /// <summary>
        /// Create a <see cref="ManualResetConfiguredProvider{T}"/>.
        /// </summary>
        public static ManualResetConfiguredProvider<T> ManualReset<T>(bool continueOnCapturedContext) => new ManualResetConfiguredProvider<T>(continueOnCapturedContext);
    }
}
