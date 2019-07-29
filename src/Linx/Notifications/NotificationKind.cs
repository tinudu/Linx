namespace Linx.Notifications
{
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
}