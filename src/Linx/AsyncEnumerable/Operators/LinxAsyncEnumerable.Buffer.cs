namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Queueing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Buffers all elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(token => new QueueingEnumerator<T>(source, new BufferAllQueue<T>(), token));
        }

        /// <summary>
        /// Buffers up to <paramref name="maxCount"/> elements if the consumer is slower than the producer.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
        /// <remarks>
        /// When the buffer is full and another element is notified, the producer experiences backpressure.
        /// </remarks>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

            return maxCount switch
            {
                0 => source,
                int.MaxValue => Create(token => new QueueingEnumerator<T>(source, new BufferAllQueue<T>(), token)),
                _ => Create(token => new QueueingEnumerator<T>(source, new BufferMaxQueue<T>(maxCount), token))
            };
        }
    }
}
