using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets access to the most recent item.
        /// </summary>
        public static IAsyncEnumerable<Deferred<Lossy<T>>> MostRecent<T>(this IAsyncEnumerable<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return new QueueingIterator<T, Lossy<T>>(source, () => new MostRecentOneQueue<T>());
        }

        /// <summary>
        /// Gets access to the most recent item, while keeping up to <paramref name="maxCapacity"/> items in a queue.
        /// </summary>
        public static IAsyncEnumerable<Deferred<Lossy<T>>> MostRecent<T>(this IAsyncEnumerable<T> source, int maxCapacity)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Must be positive.");

            Func<IQueue<T, Lossy<T>>> queueFactory = maxCapacity == 1 ?
                () => new MostRecentOneQueue<T>() :
                () => new MostRecentManyQueue<T>(maxCapacity);
            return new QueueingIterator<T, Lossy<T>>(source, queueFactory);
        }

        private sealed class MostRecentOneQueue<T> : IQueue<T, Lossy<T>>
        {
            private bool _hasValue;
            private T? _item;
            private int _ignoredCount;

            public bool Backpressure => false;

            public void Enqueue(T item)
            {
                _item = item;
                if (_hasValue)
                    checked { _ignoredCount++; }
                else
                    _hasValue = true;
            }

            public bool IsEmpty => !_hasValue;

            public Lossy<T> Dequeue()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                var result = new Lossy<T>(_item!, _ignoredCount);
                _hasValue = false;
                _item = default;
                _ignoredCount = 0;
                return result;
            }

            public void DequeueFailSafe()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                _hasValue = false;
                _item = default;
                _ignoredCount = 0;
            }
        }

        private sealed class MostRecentManyQueue<T> : QueueBase<T, (T, int), Lossy<T>>
        {
            public MostRecentManyQueue(int maxCapacity) : base(maxCapacity, false) { }

            public override bool Backpressure => false;

            public override void Enqueue(T item)
            {
                if (IsFull)
                {
                    Debug.Assert(Buffer is not null);
                    var replaced = DequeueOne();
                    EnqueueThrowIfFull(new(item, 0));
                    checked { Buffer[Offset].Item2 += replaced.Item2 + 1; }
                }
                else
                    EnqueueThrowIfFull(new(item, 0));
            }

            public override Lossy<T> Dequeue()
            {
                var tuple = DequeueOne();
                return new(tuple.Item1, tuple.Item2);
            }

            public override void DequeueFailSafe()
                => DequeueOne();
        }
    }
}
