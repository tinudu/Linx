namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Queueing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Buffers all elements if the consumer is slower than the producer.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create(token => new BufferEnumerator<T>(source, new AllQueue<T>(), token));
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
                1 => Create(token=>new BufferEnumerator<T>(source,new OneQueue<T>(),token)),
                int.MaxValue => Create(token => new BufferEnumerator<T>(source, new AllQueue<T>(), token)),
                _ => Create(token => new BufferEnumerator<T>(source, new MaxQueue<T>(maxCount), token))
            };
        }

        private sealed class BufferEnumerator<T> : IAsyncEnumerator<T>
        {
            public BufferEnumerator(IAsyncEnumerable<T> source, IQueue<T> queue, CancellationToken token)
            {
                throw new NotImplementedException();
            }

            public T Current => throw new NotImplementedException();
            public ValueTask<bool> MoveNextAsync() => throw new NotImplementedException();
            public ValueTask DisposeAsync() => throw new NotImplementedException();
        }
    }
}
