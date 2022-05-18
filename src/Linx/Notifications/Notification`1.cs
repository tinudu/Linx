using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Linx.Notifications;

/// <summary>
/// A materialized notification.
/// </summary>
[DebuggerNonUserCode]
public struct Notification<T> : IEquatable<Notification<T>>
{
    private readonly T? _value;
    private readonly Exception? _error;

    /// <summary>
    /// Gets the <see cref="NotificationKind"/>.
    /// </summary>
    public NotificationKind Kind { get; }

    /// <summary>
    /// Gets the value of a <see cref="NotificationKind.Next"/> notification.
    /// </summary>
    public T Value => Kind == NotificationKind.Next ? _value! : throw new InvalidOperationException();

    /// <summary>
    /// Gets the error of a <see cref="NotificationKind.Error"/> notification.
    /// </summary>
    public Exception Error => Kind == NotificationKind.Error ? _error! : throw new InvalidOperationException();

    internal Notification(T value)
    {
        Kind = NotificationKind.Next;
        _value = value;
        _error = null;
    }

    /// <summary>
    /// Create a <see cref="NotificationKind.Error"/> notification.
    /// </summary>
    internal Notification(Exception error)
    {
        Kind = NotificationKind.Error;
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Equality.
    /// </summary>
    public bool Equals(Notification<T> other) => NotificationComparer<T>.Default.Equals(this, other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Notification<T> other && NotificationComparer<T>.Default.Equals(this, other);

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
    public override string? ToString()
    {
        return Kind switch
        {
            NotificationKind.Next => _value?.ToString(),
            NotificationKind.Completed => "Completed",
            NotificationKind.Error => _error!.Message,
            _ => throw new Exception(Kind + "???")
        };
    }
}
