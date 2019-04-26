namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Calls the specified <paramref name="handler"/> if an error of type <typeparamref name="TException"/> occurs.
        /// </summary>
        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source, Action<TException> handler) where TException : Exception
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return Produce<TSource>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(ae.Current);
                }
                catch (TException ex) { handler(ex); }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
