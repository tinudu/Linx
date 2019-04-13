namespace Linx.Reactive.Timing
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Access to current time.
    /// </summary>
    public static class Time
    {
        private static readonly AsyncLocal<ITime> _timeProvider = new AsyncLocal<ITime>();

        /// <summary>
        /// Gets the real time.
        /// </summary>
        public static ITime RealTime { get; } = new RealTimeImpl();

        /// <summary>
        /// Gets or sets the current time.
        /// </summary>
        public static ITime Current
        {
            get => _timeProvider.Value ?? RealTime;
            set => _timeProvider.Value = value ?? RealTime;
        }

        [DebuggerStepThrough]
        private sealed class RealTimeImpl : ITime
        {
            public DateTimeOffset Now => DateTimeOffset.Now;

            public async Task Wait(TimeSpan delay, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                if (delay > TimeSpan.Zero) await Task.Delay(delay, token).ConfigureAwait(false);
            }

            public async Task Wait(DateTimeOffset due, CancellationToken token) => await Wait(due - DateTimeOffset.Now, token).ConfigureAwait(false);
        }

    }
}
