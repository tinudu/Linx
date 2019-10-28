namespace Linx.AsyncEnumerable
{
    using Notifications;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IAsyncEnumerable<Notification<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        switch (current.Kind)
                        {
                            case NotificationKind.Next:
                                if (!await yield(current.Value).ConfigureAwait(false)) return;
                                break;
                            case NotificationKind.Completed:
                                return;
                            case NotificationKind.Error:
                                throw current.Error;
                            default:
                                throw new Exception(current.Kind + "???");
                        }
                    }
                }
                finally { await ae.DisposeAsync(); }
                await token.WhenCanceled().ConfigureAwait(false);
            });
        }
    }
}
