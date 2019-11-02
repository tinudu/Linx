namespace Linx.Testing
{
    using System.Collections.Generic;
    using Notifications;

    /// <summary>
    /// <see cref="IAsyncEnumerable{T}"/> defined as a sequence of time spaced notifications.
    /// </summary>
    public interface IMarbleDiagram<T> : IAsyncEnumerable<T>
    {
        /// <summary>
        /// Gets the notifications.
        /// </summary>
        IEnumerable<TimeInterval<Notification<T>>> Marbles { get; }
    }
}
