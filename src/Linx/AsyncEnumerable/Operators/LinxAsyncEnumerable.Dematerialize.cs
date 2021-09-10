namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Notifications;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Dematerializes the explicit notification values of a sequence as implicit notifications.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IAsyncEnumerable<Notification<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Iterator();

            async IAsyncEnumerable<T> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                await foreach (var n in source.WithCancellation(token).ConfigureAwait(false))
                    switch (n.Kind)
                    {
                        case NotificationKind.Next:
                            yield return n.Value;
                            break;
                        case NotificationKind.Completed:
                            yield break;
                        case NotificationKind.Error:
                            throw n.Error;
                        default:
                            throw new Exception(n.Kind + "???");
                    }
                throw await token.WhenCancellationRequested().ConfigureAwait(false);
            }
        }
    }
}
