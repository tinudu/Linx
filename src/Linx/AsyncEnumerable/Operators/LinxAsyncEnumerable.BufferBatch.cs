using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Decouples the source from its consumer; buffered items are retrieved in batches.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static IAsyncEnumerable<Deferred<IReadOnlyList<T>>> BufferBatch<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        return new QueueingIterator<T, IReadOnlyList<T>>(source, () => new BufferBatchThrowQueue<T>(int.MaxValue));
    }

    /// <summary>
    /// Decouples the source from its consumer; buffered items are retrieved in batches.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is non-positive.</exception>
    /// <remarks>
    /// <paramref name="backpressure"/> controls what happens if <paramref name="maxCapacity"/> is reached:
    /// A value of true excerts backpressure on the source.
    /// A value of false terminates the sequence with an exception.
    /// </remarks>
    public static IAsyncEnumerable<Deferred<IReadOnlyList<T>>> BufferBatch<T>(this IAsyncEnumerable<T> source, int maxCapacity, bool backpressure)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

        Func<IQueue<T, IReadOnlyList<T>>> queueFactory = backpressure ?
            () => new BufferBatchBackpressureQueue<T>(maxCapacity) :
            () => new BufferBatchThrowQueue<T>(maxCapacity);
        return new QueueingIterator<T, IReadOnlyList<T>>(source, queueFactory);
    }

    private abstract class BufferBatchQueueBase<T> : ListQueueBase<T, IReadOnlyList<T>>
    {
        public BufferBatchQueueBase(int maxCapacity) : base(maxCapacity) { }

        // Backpressure remains abstract

        public sealed override void Enqueue(T item)
            => EnqueueThrowIfFull(item);

        public sealed override IReadOnlyList<T> Dequeue()
            => DequeueAll();

        public sealed override void DequeueFailSafe()
            => Clear();
    }

    private sealed class BufferBatchThrowQueue<T> : BufferBatchQueueBase<T>
    {
        public BufferBatchThrowQueue(int maxCapacity) : base(maxCapacity) { }

        public override bool Backpressure => false;
    }

    private sealed class BufferBatchBackpressureQueue<T> : BufferBatchQueueBase<T>
    {
        public BufferBatchBackpressureQueue(int maxCapacity) : base(maxCapacity) { }

        public override bool Backpressure => IsFull;
    }
}
