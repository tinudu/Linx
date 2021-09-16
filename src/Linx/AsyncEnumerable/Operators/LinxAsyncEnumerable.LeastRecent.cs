using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        private sealed class LeastRecentOneQueue<T> : IQueue<T, Lossy<T>>
        {
            private bool _hasValue;
            private T _item;
            private int _ignoredCount;

            public bool Backpressure => false;

            public void Enqueue(T item)
            {
                if (_hasValue)
                    checked { _ignoredCount++; }
                else
                {
                    _item = item;
                    _hasValue = true;
                }
            }

            public bool IsEmpty => !_hasValue;

            public Lossy<T> Dequeue()
            {
                if (IsEmpty) throw new InvalidOperationException(Strings.QueueIsEmpty);

                var result = new Lossy<T>(_item, _ignoredCount);
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

        private sealed class LeastRecentManyQueue<T> : QueueBase<T, (T, int), Lossy<T>>
        {
            public LeastRecentManyQueue(int maxCapacity) : base(maxCapacity, false) { }

            public override bool Backpressure => false;

            public override void Enqueue(T item)
            {
                if (IsFull)
                {
                    var ixLast = Offset - 1;
                    if (ixLast < 0) ixLast += Buffer.Length;
                    checked { Buffer[ixLast].Item2++; }
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

        private sealed class LeastRecentBatchQueue<T> : ListQueueBase<T, Lossy<IReadOnlyList<T>>>
        {
            private int _ignoredCount;

            public LeastRecentBatchQueue(int maxCapacity) : base(maxCapacity) { }

            public override bool Backpressure => false;

            public override void Enqueue(T item)
            {
                if (IsFull)
                    checked { _ignoredCount++; }
                else
                    EnqueueThrowIfFull(item);
            }

            public override Lossy<IReadOnlyList<T>> Dequeue()
            {
                var result = new Lossy<IReadOnlyList<T>>(DequeueAll(), _ignoredCount);
                _ignoredCount = 0;
                return result;
            }

            public override void DequeueFailSafe()
            {
                Clear();
                _ignoredCount = 0;
            }
        }
    }
}
