namespace Linx.Observable
{
    using System;
    using System.Collections.Generic;
    using AsyncEnumerable;
    using Queueing;

    partial class LinxObservable
    {
        /// <summary>
        /// Buffers all elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new BufferAllQueue<T>(), token));
        }

        /// <summary>
        /// Buffers up to <paramref name="maxCount"/> elements if the consumer is slower than the producer.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCount"/> is negative.</exception>
        /// <remarks>
        /// When the buffer is full and another element is notified, the sequence completes with a <see cref="BufferOverflowException"/>.
        /// </remarks>
        public static IAsyncEnumerable<T> Buffer<T>(this ILinxObservable<T> source, int maxCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

            return maxCount switch
            {
                0 => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, BufferNoneQueue<T>.Instance, token)),
                int.MaxValue => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new BufferAllQueue<T>(), token)),
                _ => LinxAsyncEnumerable.Create(token => new QueueingEnumerator<T>(source, new BufferMaxQueue<T>(maxCount), token))
            };
        }
    }
}
