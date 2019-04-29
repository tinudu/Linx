namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Materializes the implicit notifications of an observable sequence as explicit notification values.
        /// </summary>
        public static IAsyncEnumerable<INotification<T>> Materialize<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<INotification<T>>(async (yield, token) =>
            {
                INotification<T> completion;
                try
                {
                    var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (await ae.MoveNextAsync())
                            await yield(Notification.OnNext(ae.Current));
                    }
                    finally { await ae.DisposeAsync(); }

                    completion = Notification.OnCompleted<T>();
                }
                catch (Exception ex) { completion = Notification.OnError<T>(ex); }

                await yield(completion);
            });
        }
    }
}
