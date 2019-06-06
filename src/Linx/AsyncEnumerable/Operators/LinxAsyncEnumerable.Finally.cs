namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Invokes a specified action after source observable sequence terminates normally or by an exception.
        /// </summary>
        public static IAsyncEnumerable<T> Finally<T>(this IAsyncEnumerable<T> source, Action @finally)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (@finally == null) throw new ArgumentNullException(nameof(@finally));

            return Produce<T>(async (yield, token) =>
            {
                try { await source.CopyTo(yield, token).ConfigureAwait(false); }
                finally { @finally(); }
            });
        }

        /// <summary>
        /// Invokes a specified async action after source observable sequence terminates normally or by an exception.
        /// </summary>
        public static IAsyncEnumerable<T> Finally<T>(this IAsyncEnumerable<T> source, Func<Task> @finally)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (@finally == null) throw new ArgumentNullException(nameof(@finally));

            return Produce<T>(async (yield, token) =>
            {
                try { await source.CopyTo(yield, token).ConfigureAwait(false); }
                finally { await @finally().ConfigureAwait(false); }
            });
        }
    }
}
