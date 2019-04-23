namespace Linx.Reactive
{
    using System;

    /// <summary>
    /// Kind of a <see cref="INotification{T}"/>.
    /// </summary>
    public enum NotificationKind : byte
    {
        /// <summary>
        /// Notifiaction representing a sequence element.
        /// </summary>
        OnNext,

        /// <summary>
        /// Notification representing an error.
        /// </summary>
        OnError,

        /// <summary>
        /// Notification representing the end of the sequence.
        /// </summary>
        OnCompleted
    }

    /// <summary>
    /// A materialized notification.
    /// </summary>
    public interface INotification<out T>
    {
        /// <summary>
        /// Gets the <see cref="NotificationKind"/>.
        /// </summary>
        NotificationKind Kind { get; }

        /// <summary>
        /// Gets the value of a <see cref="NotificationKind.OnNext"/> notification.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets the error of a <see cref="NotificationKind.OnError"/> notifiacation.
        /// </summary>
        Exception Error { get; }
    }

    /// <summary>
    /// Static factory methods for <see cref="INotification{T}"/>.
    /// </summary>
    public static class Notification
    {
        /// <summary>
        /// Creates a <see cref="NotificationKind.OnNext"/> notification.
        /// </summary>
        public static INotification<T> OnNext<T>(T value) => new OnNextNotification<T>(value);

        /// <summary>
        /// Creates a <see cref="NotificationKind.OnError"/> notification.
        /// </summary>
        public static INotification<T> OnError<T>(Exception error) => new OnErrorNotification<T>(error);

        /// <summary>
        /// Creates a <see cref="NotificationKind.OnCompleted"/> notification.
        /// </summary>
        public static INotification<T> OnCompleted<T>() => OnCompletedNotification<T>.Default;

        private sealed class OnNextNotification<T> : INotification<T>
        {
            public NotificationKind Kind => NotificationKind.OnNext;
            public T Value { get; }
            public Exception Error => null;

            public OnNextNotification(T value) => Value = value;

            public override string ToString() => $"OnNext({Value})";
        }

        private sealed class OnErrorNotification<T> : INotification<T>
        {
            public NotificationKind Kind => NotificationKind.OnError;
            public T Value => default;
            public Exception Error { get; }

            public OnErrorNotification(Exception error) => Error = error ?? throw new ArgumentNullException(nameof(error));

            public override string ToString() => $"OnError({Error})";
        }

        private sealed class OnCompletedNotification<T> : INotification<T>
        {
            public static OnCompletedNotification<T> Default { get; } = new OnCompletedNotification<T>();
            private OnCompletedNotification() { }

            public NotificationKind Kind => NotificationKind.OnCompleted;
            public T Value => default;
            public Exception Error => null;

            public override string ToString() => $"OnCompleted()";
        }
    }
}
