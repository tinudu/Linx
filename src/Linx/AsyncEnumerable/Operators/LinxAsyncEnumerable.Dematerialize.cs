namespace Linx.AsyncEnumerable
{
    using Notifications;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Dematerializes the explicit notification values of a sequence as implicit notifications.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IAsyncEnumerable<Notification<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(GetEnumerator);

            async IAsyncEnumerator<T> GetEnumerator(CancellationToken token)
            {
                await foreach (var n in source.WithCancellation(token).ConfigureAwait(false))
                    switch (n.Kind)
                    {
                        case NotificationKind.Next:
                            yield return n.Value;
                            continue;
                        case NotificationKind.Completed:
                            yield break;
                        case NotificationKind.Error:
                            throw n.Error;
                        default:
                            throw new Exception(n.Kind + "???");
                    }
                throw await token.WhenCanceledAsync().ConfigureAwait(false);
            }
        }
    }
}
