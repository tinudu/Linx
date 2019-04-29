namespace Linx.AsyncEnumerable.Timing
{
    using System.Threading;

    /// <summary>
    /// Access to current time.
    /// </summary>
    public static class Time
    {
        private static readonly AsyncLocal<ITime> _timeProvider = new AsyncLocal<ITime>();

        /// <summary>
        /// Gets or sets the current time.
        /// </summary>
        public static ITime Current
        {
            get => _timeProvider.Value ?? RealTime.Instance;
            internal set => _timeProvider.Value = value;
        }
    }
}
