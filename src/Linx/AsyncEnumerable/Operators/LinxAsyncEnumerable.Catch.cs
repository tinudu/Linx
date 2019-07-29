namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Catches exceptions of type <typeparamref name="TException"/>.
        /// </summary>
        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source) where TException : Exception
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<TSource>(async (yield, token) =>
            {
                try { await source.CopyTo(yield, token).ConfigureAwait(false); }
                catch (TException) { /**/ }
            });
        }

        /// <summary>
        /// Catches exceptions of type <typeparamref name="TException"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// IAsyncEnumerable&lt;int> source = ...;
        /// var catched = source
        ///     .Select(i => new { X = i, Y = 2 * i }) // anonymous type
        ///     .Timeout(TimeSpan.FromSeconds(5))
        ///     .Catch(default(TimeoutException)); // Catch&lt;anonymous, TimeoutException>(...)
        /// </code>
        /// </example>
        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source, TException sample) where TException : Exception
            => source.Catch<TSource, TException>();

        /// <summary>
        /// Catches exceptions of type <typeparamref name="TException"/> and replaces it with the sequence returned by the specified <paramref name="handler"/>.
        /// </summary>
        public static IAsyncEnumerable<TSource> Catch<TSource, TException>(this IAsyncEnumerable<TSource> source, Func<TException, IAsyncEnumerable<TSource>> handler) where TException : Exception
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            return Create<TSource>(async (yield, token) =>
            {
                try { await source.CopyTo(yield, token).ConfigureAwait(false); }
                catch (TException ex) { await handler(ex).CopyTo(yield, token).ConfigureAwait(false); }
            });
        }
    }
}
