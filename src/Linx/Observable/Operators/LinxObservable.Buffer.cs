namespace Linx.Observable
{
    using AsyncEnumerable;
    using System;
    using System.Collections.Generic;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Buffers items in case the consumer is slower than the generator.
        /// </summary>
        public static IAsyncEnumerable<T> Buffer<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return LinxAsyncEnumerable.Create(token => new BufferAllEnumerator<T>(source, token));
        }

        private sealed class BufferAllEnumerator<T> : BufferEnumeratorBase<T, T>
        {
            public BufferAllEnumerator(ILinxObservable<T> source, CancellationToken token) : base(source, token)
            {
            }

            protected override void Enqueue(T item) => Queue.Enqueue(item);
            protected override T Dequeue() => Queue.Dequeue();
            protected override void Prune() { }
        }
    }
}
