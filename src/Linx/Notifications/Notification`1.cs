namespace Linx.Notifications
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A materialized notification.
    /// </summary>
    [DebuggerNonUserCode]
    public struct Notification<T> : IEquatable<Notification<T>>
    {
        /// <summary>
        /// Gets the <see cref="NotificationKind"/>.
        /// </summary>
        public NotificationKind Kind { get; }

        /// <summary>
        /// Gets the value of a <see cref="NotificationKind.Next"/> notification.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the error of a <see cref="NotificationKind.Error"/> notification.
        /// </summary>
        public Exception Error { get; }

        internal Notification(T value)
        {
            Kind = NotificationKind.Next;
            Value = value;
            Error = null;
        }

        /// <summary>
        /// Create a <see cref="NotificationKind.Error"/> notification.
        /// </summary>
        internal Notification(Exception error)
        {
            Kind = NotificationKind.Error;
            Value = default;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        /// <summary>
        /// Equality.
        /// </summary>
        public bool Equals(Notification<T> other) => NotificationComparer<T>.Default.Equals(this, other);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Notification<T> other && NotificationComparer<T>.Default.Equals(this, other);

        /// <summary>
        /// Equality.
        /// </summary>
        public static bool operator ==(Notification<T> left, Notification<T> right) => NotificationComparer<T>.Default.Equals(left, right);

        /// <summary>
        /// Inequality.
        /// </summary>
        public static bool operator !=(Notification<T> left, Notification<T> right) => !NotificationComparer<T>.Default.Equals(left, right);

        /// <inheritdoc />
        public override int GetHashCode() => NotificationComparer<T>.Default.GetHashCode(this);

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind switch
            {
                NotificationKind.Next => (Value?.ToString() ?? string.Empty),
                NotificationKind.Completed => "Completed",
                NotificationKind.Error => $"{Error.GetType().Name}({Error.Message})",
                _ => throw new Exception(Kind + "???")
            };
        }
    }
}
