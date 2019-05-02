namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Kind of a <see cref="Notification{T}"/>.
    /// </summary>
    public enum NotificationKind : byte
    {
        /// <summary>
        /// Notification representing the end of the sequence.
        /// </summary>
        Completed,

        /// <summary>
        /// Notifiaction representing a sequence element.
        /// </summary>
        Next,

        /// <summary>
        /// Notification representing an error.
        /// </summary>
        Error
    }

    /// <summary>
    /// A materialized notification.
    /// </summary>
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
        /// <remarks>
        /// <see cref="NotificationKind.Next"/> notifications are compared using the <see cref="EqualityComparer{T}.Default"/>.
        /// <see cref="NotificationKind.Error"/> notifications are compared by exception type and message.
        /// </remarks>
        public bool Equals(Notification<T> other)
        {
            if (other.Kind != Kind) return false;

            switch (Kind)
            {
                case NotificationKind.Next:
                    return other.Kind == NotificationKind.Next && EqualityComparer<T>.Default.Equals(Value, other.Value);
                case NotificationKind.Completed:
                    return other.Kind == NotificationKind.Completed;
                case NotificationKind.Error:
                    return other.Kind == NotificationKind.Error && Error.GetType() == other.Error.GetType() && Error.Message == other.Error.Message;
                default:
                    throw new Exception(Kind + "???");
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Notification<T> n && Equals(n);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            switch (Kind)
            {
                case NotificationKind.Next:
                    return new HashCode() + (int)NotificationKind.Next + EqualityComparer<T>.Default.GetHashCode(Value);
                case NotificationKind.Completed:
                    return 0;
                case NotificationKind.Error:
                    return new HashCode() + (int)NotificationKind.Error + Error.GetType().GetHashCode() + Error.Message.GetHashCode();
                default:
                    throw new Exception(Kind + "???");
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Kind)
            {
                case NotificationKind.Next:
                    return Value?.ToString() ?? string.Empty;
                case NotificationKind.Completed:
                    return "Completed";
                case NotificationKind.Error:
                    return $"{Error.GetType().Name}({Error.Message})";
                default:
                    throw new Exception(Kind + "???");
            }
        }
    }

    /// <summary>
    /// <see cref="Notification{T}"/> factory methods.
    /// </summary>
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
