namespace Linx.AsyncEnumerable.Timing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Notion of time, real or virtual.
    /// </summary>
    public interface ITime
    {
        /// <summary>
        /// Gets the current time.
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// Delay for the specified interval.
        /// </summary>
        Task Delay(TimeSpan delay, CancellationToken token);

        /// <summary>
        /// Delay until the specified time is reached.
        /// </summary>
        Task Delay(DateTimeOffset due, CancellationToken token);

        /// <summary>
        /// Create a timer.
        /// </summary>
        ITimer GetTimer(CancellationToken token);
    }
}
