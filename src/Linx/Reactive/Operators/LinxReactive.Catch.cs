namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Invokes the specified action when the sequence terminates with an exception of type <typeparamref name="TException"/>.
        /// </summary>
        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source, Action<TException> handler) where TException : Exception
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return Produce<TSource>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(ae.Current);
                }
                catch (TException ex) { handler(ex); }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
