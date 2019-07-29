namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Notifications;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Materializes the implicit notifications of an observable sequence as explicit notification values.
        /// </summary>
        public static IAsyncEnumerable<Notification<T>> Materialize<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<Notification<T>>(async (yield, token) =>
            {
                Notification<T> completion;
                try
                {
                    var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (await ae.MoveNextAsync())
                            if (!await yield(Notification.Next(ae.Current)).ConfigureAwait(false))
                                return;
                    }
                    finally { await ae.DisposeAsync(); }

                    completion = Notification.Completed<T>();
                }
                catch (Exception ex) { completion = Notification.Error<T>(ex); }

                await yield(completion).ConfigureAwait(false);
            });
        }
    }
}
