namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Materializes the implicit notifications of an observable sequence as explicit notification values.
        /// </summary>
        public static IAsyncEnumerableObs<INotification<T>> Materialize<T>(this IAsyncEnumerableObs<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<INotification<T>>(async (yield, token) =>
            {
                INotification<T> completion;
                try
                {
                    var ae = source.GetAsyncEnumerator(token);
                    try
                    {
                        while (await ae.MoveNextAsync())
                            await yield(Notification.OnNext(ae.Current));
                    }
                    finally { await ae.DisposeAsync().ConfigureAwait(false); }

                    completion = Notification.OnCompleted<T>();
                }
                catch (Exception ex) { completion = Notification.OnError<T>(ex); }

                await yield(completion);
            });
        }
    }
}
