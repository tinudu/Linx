namespace Linx.AsyncEnumerable
{
    using Notifications;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
                    await foreach (var item in source.WithCancellation(token).ConfigureAwait(false))
                        if (!await yield(Notification.Next(item)).ConfigureAwait(false))
                            return;
                    completion = Notification.Completed<T>();
                }
                catch (Exception ex) { completion = Notification.Error<T>(ex); }

                await yield(completion).ConfigureAwait(false);
            });
        }
    }
}
