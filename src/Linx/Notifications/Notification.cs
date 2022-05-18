using System;
using System.Diagnostics;

namespace Linx.Notifications;

/// <summary>
/// <see cref="Notification{T}"/> factory methods.
/// </summary>
[DebuggerNonUserCode]
public static class Notification
{
    /// <summary>
    /// Create a <see cref="NotificationKind.Next"/> notification.
    /// </summary>
    public static Notification<T> Next<T>(T value) => new(value);

    /// <summary>
    /// Create a <see cref="NotificationKind.Completed"/> notification.
    /// </summary>
    public static Notification<T> Completed<T>() => new();

    /// <summary>
    /// Create a <see cref="NotificationKind.Error"/> notification.
    /// </summary>
    public static Notification<T> Error<T>(Exception error) => new(error);
}
