namespace Linx.Notifications
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="IEqualityComparer{T}"/> for notifications.
    /// </summary>
    public sealed class NotificationComparer<T> : IEqualityComparer<Notification<T>>
    {
        /// <summary>
        /// Singleton using <see cref="EqualityComparer{T}.Default"/> to compare values.
        /// </summary>
        public static NotificationComparer<T> Default { get; } = new NotificationComparer<T>(EqualityComparer<T>.Default, EqualityComparer<Exception>.Default);

        /// <summary>
        /// Gets a <see cref="NotificationComparer{T}"/> using the specified <paramref name="valueComparer"/>.
        /// </summary>
        public static NotificationComparer<T> GetComparer(IEqualityComparer<T>? valueComparer, IEqualityComparer<Exception>? errorComparer)
        {
            if (valueComparer is null) valueComparer = EqualityComparer<T>.Default;
            if (errorComparer is null) errorComparer = EqualityComparer<Exception>.Default;

            return ReferenceEquals(valueComparer, EqualityComparer<T>.Default) && ReferenceEquals(errorComparer, EqualityComparer<Exception>.Default) ?
                Default :
                new NotificationComparer<T>(valueComparer, errorComparer);
        }

        private readonly IEqualityComparer<T> _valueComparer;
        private readonly IEqualityComparer<Exception> _errorComparer;

        private NotificationComparer(IEqualityComparer<T> valueComparer, IEqualityComparer<Exception> errorComparer)
        {
            _valueComparer = valueComparer;
            _errorComparer = errorComparer;
        }

        /// <inheritdoc />
        public bool Equals(Notification<T> x, Notification<T> y) => x.Kind switch
        {
            NotificationKind.Completed => y.Kind == NotificationKind.Completed,
            NotificationKind.Next => y.Kind == NotificationKind.Next && _valueComparer.Equals(x.Value, y.Value),
            NotificationKind.Error => x.Kind == NotificationKind.Error && _errorComparer.Equals(x.Error, y.Error),
            _ => throw new Exception(x.Kind + "???")
        };

        /// <inheritdoc />
        public int GetHashCode(Notification<T> n)
        {
            var hc = new HashCode();
            hc.Add(n.Kind);
            switch (n.Kind)
            {
                case NotificationKind.Completed:
                    break;

                case NotificationKind.Next:
                    hc.Add(n.Value, _valueComparer);
                    break;

                case NotificationKind.Error:
                    hc.Add(n.Error, _errorComparer);
                    break;

                default:
                    throw new Exception(n.Kind + "???");
            }
            return hc.ToHashCode();
        }
    }
}
