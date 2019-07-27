namespace Linx.AsyncEnumerable.Notifications
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="IEqualityComparer{T}"/> for notifications.
    /// </summary>
    /// <remarks>
    /// Compares values with a specific <see cref="IEqualityComparer{T}"/> for the type <typeparamref name="T"/>.
    /// Compares errors by type and <see cref="Exception.Message"/>.
    /// </remarks>
    public sealed class NotificationComparer<T> : IEqualityComparer<Notification<T>>
    {
        /// <summary>
        /// Singleton using <see cref="EqualityComparer{T}.Default"/> to compare values.
        /// </summary>
        public static NotificationComparer<T> Default { get; } = new NotificationComparer<T>(EqualityComparer<T>.Default);

        /// <summary>
        /// Gets a <see cref="NotificationComparer{T}"/> using the specified <paramref name="valueComparer"/>.
        /// </summary>
        /// <param name="valueComparer">Optional, defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
        public static NotificationComparer<T> GetComparer(IEqualityComparer<T> valueComparer) => valueComparer == null || ReferenceEquals(valueComparer, EqualityComparer<T>.Default) ? Default : new NotificationComparer<T>(valueComparer);

        private readonly IEqualityComparer<T> _valueComparer;

        private NotificationComparer(IEqualityComparer<T> valueComparer) => _valueComparer = valueComparer;

        /// <inheritdoc />
        public bool Equals(Notification<T> x, Notification<T> y)
        {
            switch (x.Kind)
            {
                case NotificationKind.Completed:
                    return y.Kind == NotificationKind.Completed;
                case NotificationKind.Next:
                    return y.Kind == NotificationKind.Next && (x.Value == null ? y.Value == null : y.Value != null && _valueComparer.Equals(x.Value, y.Value));
                case NotificationKind.Error:
                    return x.Kind == NotificationKind.Error && x.Error.GetType() == y.Error.GetType() && x.Error.Message == y.Error.Message;
                default:
                    throw new Exception(x.Kind + "???");
            }
        }

        /// <inheritdoc />
        public int GetHashCode(Notification<T> n)
        {
            switch (n.Kind)
            {
                case NotificationKind.Completed:
                    return 0;
                case NotificationKind.Next:
                    return n.Value == null ? 0 : _valueComparer.GetHashCode(n.Value);
                case NotificationKind.Error:
                    return new HashCode().Hash(n.Error.GetType()).Hash(n.Error.Message);
                default:
                    throw new Exception(n.Kind + "???");
            }
        }
    }
}
