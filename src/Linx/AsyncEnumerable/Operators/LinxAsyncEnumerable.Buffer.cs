﻿using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Decouples the source from its consumer; buffered items are retrieved individually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new QueueingIterator<T, T>(source, () => new BufferThrowQueue<T>(int.MaxValue)).Select(dd => dd.GetResult());
        }

        /// <summary>
        /// Decouples the source from its consumer; buffered items are retrieved individually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is non-positive.</exception>
        /// <remarks>
        /// <paramref name="backpressure"/> controls what happens if <paramref name="maxCapacity"/> is reached:
        /// A value of true excerts backpressure on the source.
        /// A value of false terminates the sequence with an exception.
        /// </remarks>
        public static IAsyncEnumerable<T> Buffer<T>(this IAsyncEnumerable<T> source, int maxCapacity, bool backpressure)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            Func<IQueue<T, T>> queueFactory = backpressure ?
                () => new BufferBackpressureQueue<T>(maxCapacity) :
                () => new BufferThrowQueue<T>(maxCapacity);
            return new QueueingIterator<T, T>(source, queueFactory).Select(dd => dd.GetResult());
        }

        private abstract class BufferQueueBase<T> : QueueBase<T, T, T>
        {
            public BufferQueueBase(int maxCapacity) : base(maxCapacity, false) { }

            // Backpressure remains abstract

            public override sealed void Enqueue(T item)
                => EnqueueThrowIfFull(item);

            public override sealed T Dequeue()
                => DequeueOne();

            public override sealed void DequeueFailSafe()
                => DequeueOne();
        }

        private sealed class BufferThrowQueue<T> : BufferQueueBase<T>
        {
            public BufferThrowQueue(int maxCapacity) : base(maxCapacity) { }

            public override bool Backpressure => false;
        }

        private sealed class BufferBackpressureQueue<T> : BufferQueueBase<T>
        {
            public BufferBackpressureQueue(int maxCapacity) : base(maxCapacity) { }

            public override bool Backpressure => IsFull;
        }
    }
}
