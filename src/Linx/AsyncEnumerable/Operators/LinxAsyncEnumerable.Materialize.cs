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
        public static IAsyncEnumerable<Notification<T>> Materialize<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<Notification<T>>(async (yield, token) =>
            {
                Notification<T> completion;
                try
                {
                    var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while (await ae.MoveNextAsync())
                            await yield(new Notification<T>(ae.Current));
                    }
                    finally { await ae.DisposeAsync(); }

                    completion = new Notification<T>();
                }
                catch (Exception ex) { completion = new Notification<T>(ex); }

                await yield(completion);
            });
        }
    }
}
