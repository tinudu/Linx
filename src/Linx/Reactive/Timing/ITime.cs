namespace Linx.Reactive.Timing
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
        /// Wait for the specified amount of time.
        /// </summary>
        Task Delay(TimeSpan delay, CancellationToken token);

        /// <summary>
        /// Delay until <paramref name="due"/> is reached.
        /// </summary>
        Task Delay(DateTimeOffset due, CancellationToken token);

        /// <summary>
        /// Create a new timer.
        /// </summary>
        /// <param name="onElapsed">Delegate to be called when the timer elapses.</param>
        ITimer CreateTimer(TimerElapsedDelegte onElapsed);
    }
}
