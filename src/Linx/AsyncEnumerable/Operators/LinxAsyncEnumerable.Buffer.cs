﻿namespace Linx.AsyncEnumerable
{
    using global::Linx.Queueing;
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Buffers all elements.
        /// </summary>
        public static IAsyncEnumerable<QueueReader<T>> Buffer<T>(this IAsyncEnumerable<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.Queue(QueueFactory.Buffer<T>, true);
        }

        /// <summary>
        /// Buffers up to <paramref name="maxSize"/> elements.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSize"/> must greater than 1.</exception>
        public static IAsyncEnumerable<QueueReader<T>> Buffer<T>(this IAsyncEnumerable<T> source, int maxSize, bool throwOnQueueFull)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxSize <= 1) throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be > 1.");

            return source.Queue(() => QueueFactory.Buffer<T>(maxSize), throwOnQueueFull);
        }
    }
}
