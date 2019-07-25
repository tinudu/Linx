namespace Linx.Timing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

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
        /// Schedule the specified <paramref name="action"/>.
        /// </summary>
        public static async Task Schedule(this ITime time, Action action, int dueMillis, CancellationToken token = default)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (action == null) throw new ArgumentNullException(nameof(action));

            await time.Delay(dueMillis, token).ConfigureAwait(false);
            action();
        }

        /// <summary>
        /// Schedule the specified <paramref name="action"/>.
        /// </summary>
        public static async Task Schedule(this ITime time, Action action, TimeSpan due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            action();
        }

        /// <summary>
        /// Schedule the specified <paramref name="action"/>.
        /// </summary>
        public static async Task Schedule(this ITime time, Action action, DateTimeOffset due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            action();
        }

        /// <summary>
        /// Schedule the specified <paramref name="action"/>.
        /// </summary>
        public static async Task Schedule(this ITime time, Func<Task> action, int dueMillis, CancellationToken token = default)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (action == null) throw new ArgumentNullException(nameof(action));

            await time.Delay(dueMillis, token).ConfigureAwait(false);
            await action().ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule the specified <paramref name="action"/>.
        /// </summary>
        public static async Task Schedule(this ITime time, Func<Task> action, TimeSpan due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            await action().ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule the specified <paramref name="action"/>.
        /// </summary>
        public static async Task Schedule(this ITime time, Func<Task> action, DateTimeOffset due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            await action().ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule the specified <paramref name="function"/>.
        /// </summary>
        public static async Task<T> Schedule<T>(this ITime time, Func<T> function, int dueMillis, CancellationToken token = default)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (function == null) throw new ArgumentNullException(nameof(function));

            await time.Delay(dueMillis, token).ConfigureAwait(false);
            return function();
        }

        /// <summary>
        /// Schedule the specified <paramref name="function"/>.
        /// </summary>
        public static async Task<T> Schedule<T>(this ITime time, Func<T> function, TimeSpan due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            return function();
        }

        /// <summary>
        /// Schedule the specified <paramref name="function"/>.
        /// </summary>
        public static async Task<T> Schedule<T>(this ITime time, Func<T> function, DateTimeOffset due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            return function();
        }

        /// <summary>
        /// Schedule the specified <paramref name="function"/>.
        /// </summary>
        public static async Task<T> Schedule<T>(this ITime time, Func<Task<T>> function, int dueMillis, CancellationToken token = default)
        {
            if (time == null) throw new ArgumentNullException(nameof(time));
            if (function == null) throw new ArgumentNullException(nameof(function));

            await time.Delay(dueMillis, token).ConfigureAwait(false);
            return await function().ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule the specified <paramref name="function"/>.
        /// </summary>
        public static async Task<T> Schedule<T>(this ITime time, Func<Task<T>> function, TimeSpan due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            return await function().ConfigureAwait(false);
        }

        /// <summary>
        /// Schedule the specified <paramref name="function"/>.
        /// </summary>
        public static async Task<T> Schedule<T>(this ITime time, Func<Task<T>> function, DateTimeOffset due, CancellationToken token = default)
        {
            await time.Delay(due, token).ConfigureAwait(false);
            return await function().ConfigureAwait(false);
        }
    }
}
