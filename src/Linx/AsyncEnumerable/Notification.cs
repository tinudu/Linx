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
        OnCompleted,

        /// <summary>
        /// Notification representing an error.
        /// </summary>
        OnError,

        /// <summary>
        /// Notifiaction representing a sequence element.
        /// </summary>
        OnNext,
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
        /// Gets the value of a <see cref="NotificationKind.OnNext"/> notification.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets the error of a <see cref="NotificationKind.OnError"/> notification.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Create a <see cref="NotificationKind.OnNext"/> notification.
        /// </summary>
        public Notification(T value)
        {
            Kind = NotificationKind.OnNext;
            Value = value;
            Error = null;
        }

        /// <summary>
        /// Create a <see cref="NotificationKind.OnError"/> notification.
        /// </summary>
        public Notification(Exception error)
        {
            Kind = NotificationKind.OnError;
            Value = default;
            Error = error ?? throw new ArgumentNullException(nameof(error));
        }

        /// <summary>
        /// Equality.
        /// </summary>
        /// <remarks>
        /// <see cref="NotificationKind.OnNext"/> notifications are compared using the <see cref="EqualityComparer{T}.Default"/>.
        /// <see cref="NotificationKind.OnError"/> notifications are compared by exception type and message.
        /// </remarks>
        public bool Equals(Notification<T> other)
        {
            if (other.Kind != Kind) return false;

            switch (Kind)
            {
                case NotificationKind.OnNext:
                    return other.Kind == NotificationKind.OnNext && EqualityComparer<T>.Default.Equals(Value, other.Value);
                case NotificationKind.OnCompleted:
                    return other.Kind == NotificationKind.OnCompleted;
                case NotificationKind.OnError:
                    return other.Kind == NotificationKind.OnError && Error.GetType() == other.Error.GetType() && Error.Message == other.Error.Message;
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
                case NotificationKind.OnNext:
                    return new HashCode() + (int)NotificationKind.OnNext + EqualityComparer<T>.Default.GetHashCode(Value);
                case NotificationKind.OnCompleted:
                    return 0;
                case NotificationKind.OnError:
                    return new HashCode() + (int)NotificationKind.OnError + Error.GetType().GetHashCode() + Error.Message.GetHashCode();
                default:
                    throw new Exception(Kind + "???");
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Kind)
            {
                case NotificationKind.OnNext:
                    return $"OnNext({Value})";
                case NotificationKind.OnCompleted:
                    return "OnCompleted()";
                case NotificationKind.OnError:
                    return $"OnError({Error.Message})";
                default:
                    throw new Exception(Kind + "???");
            }
        }
    }
}
