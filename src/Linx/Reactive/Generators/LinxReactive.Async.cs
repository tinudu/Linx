﻿namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;

    partial class LinxReactive
    {
        /// <summary>
        /// Convert a <see cref="IEnumerable{T}"/> to a <see cref="IAsyncEnumerableObs{T}"/>.
        /// </summary>
        public static IAsyncEnumerableObs<T> Async<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                foreach (var element in source)
                    await yield(element);
            });
        }
    }
}
