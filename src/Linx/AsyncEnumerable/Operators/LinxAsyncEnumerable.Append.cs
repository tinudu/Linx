namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Appends a value to the end of the sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Append<T>(this IAsyncEnumerable<T> source, T element)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<T>(async (yield, token) =>
            {
                if (!await source.CopyTo(yield, token).ConfigureAwait(false)) return;
                await yield(element).ConfigureAwait(false);
            });
        }
    }
}
