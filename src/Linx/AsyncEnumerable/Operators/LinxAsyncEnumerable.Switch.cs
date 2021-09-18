namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Transforms a sequence of sequences into an sequence producing values only from the most recent sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Switch<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            throw new NotImplementedException("");
        }
    }
}
