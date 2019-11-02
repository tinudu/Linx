namespace Linx.Timing
{
    using System;
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

        /// <summary>
        /// Schedule an action.
        /// </summary>
        public static async void Schedule(this ITime time, Action action, DateTimeOffset due, CancellationToken token)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (token.IsCancellationRequested) return;

            try
            {
                await time.Delay(due, token).ConfigureAwait(false);
                action();
            }
            catch{ /*ignore*/}
        }

        /// <summary>
        /// Schedule an action.
        /// </summary>
        public static async void Schedule(this ITime time, Action action, TimeSpan due, CancellationToken token)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (token.IsCancellationRequested) return;

            try
            {
                await time.Delay(due, token).ConfigureAwait(false);
                action();
            }
            catch { /*ignore*/}
        }
    }
}
