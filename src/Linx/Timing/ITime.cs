namespace Linx.Timing
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
       ValueTask Delay(TimeSpan due, CancellationToken token);

        /// <summary>
        /// Delay for the specified interval in milliseconds.
        /// </summary>
        ValueTask Delay(int dueMillis, CancellationToken token);

        /// <summary>
        /// Delay until the specified time is reached.
        /// </summary>
        ValueTask Delay(DateTimeOffset due, CancellationToken token);

        /// <summary>
        /// Create a timer.
        /// </summary>
        ITimer GetTimer(CancellationToken token);
    }
}
