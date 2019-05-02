namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Emits <paramref name="finally"/> after <paramref name="source"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Finally<T>(this IAsyncEnumerable<T> source, IAsyncEnumerable<T> @finally)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (@finally == null) throw new ArgumentNullException(nameof(@finally));

            return Produce<T>(async (yield, token) =>
            {
                try { await source.CopyTo(yield, token); }
                finally { await @finally.CopyTo(yield, token); }
            });

        }
    }
}
