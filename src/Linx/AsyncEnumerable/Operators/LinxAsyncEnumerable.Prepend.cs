﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Prepends a value to the end of the sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Prepend<T>(this IAsyncEnumerable<T> source, T element)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                if (!await yield(element).ConfigureAwait(false)) return;
                await source.CopyTo(yield, token).ConfigureAwait(false);
            });
        }
    }
}
