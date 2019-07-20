namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Catches exceptions of type <typeparamref name="TException"/> and replaces it with the sequence returned by the specified <paramref name="handler"/>.
        /// </summary>
        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source, Func<TException, IAsyncEnumerable<TSource>> handler) where TException : Exception
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return Generate<TSource>(async (yield, token) =>
            {
                try { await source.CopyTo(yield, token).ConfigureAwait(false); }
                catch (TException ex) { await handler(ex).CopyTo(yield, token).ConfigureAwait(false); }
            });
        }
    }
}
