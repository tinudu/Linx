namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
        /// </summary>
        public static IAsyncEnumerable<T> Dematerialize<T>(this IAsyncEnumerable<INotification<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        switch (current.Kind)
                        {
                            case NotificationKind.OnNext:
                                await yield(current.Value);
                                break;
                            case NotificationKind.OnCompleted:
                                return;
                            case NotificationKind.OnError:
                                throw current.Error;
                            default:
                                throw new Exception(current.Kind + "???");
                        }
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
