namespace Linx.AsyncEnumerable.Timing
{
    using System;

    /// <summary>
    /// Callback delegate for a <see cref="ITimer"/>.
    /// </summary>
    /// <param name="timer">The <see cref="ITimer"/> that elapsed.</param>
    /// <param name="time">The time the timer was due.</param>
    public delegate void TimerElapsedDelegte(ITimer timer, DateTimeOffset time);

    /// <summary>
    /// Abstraction of a timer.
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// Enables the timer to fire at the specified delay from now.
        /// </summary>
        void Enable(TimeSpan delay);

        /// <summary>
        /// Enables the timer to fire at the specified time.
        /// </summary>
        void Enable(DateTimeOffset due);

        /// <summary>
        /// Disables the timer.
        /// </summary>
        void Disable();
    }
}
