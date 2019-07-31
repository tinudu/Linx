namespace Linx.Notifications
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// <see cref="Notification{T}"/> factory methods.
    /// </summary>
    [DebuggerNonUserCode]
    public static class Notification
    {
        /// <summary>
        /// Create a <see cref="NotificationKind.Next"/> notification.
        /// </summary>
        public static Notification<T> Next<T>(T value) => new Notification<T>(value);

        /// <summary>
        /// Create a <see cref="NotificationKind.Completed"/> notification.
        /// </summary>
        public static Notification<T> Completed<T>() => new Notification<T>();

        /// <summary>
        /// Create a <see cref="NotificationKind.Error"/> notification.
        /// </summary>
        public static Notification<T> Error<T>(Exception error) => new Notification<T>(error);
    }
}
