namespace Linx.Timing
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Abstraction of a timer.
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ITime"/>.
        /// </summary>
        ITime Time { get; }

        /// <summary>
        /// Delay for the specified interval.
        /// </summary>
        ValueTask Delay(TimeSpan due);

        /// <summary>
        /// Delay until the specified time is reached.
        /// </summary>
        ValueTask Delay(DateTimeOffset due);
    }
}
