namespace Linx.Collections
{
    using System;
    using System.Collections.Generic;

    partial class Queue
    {
        private sealed class LatestOneQueue<T> : IQueue<T>
        {
            private Maybe<T> _maybe;

            public int Count => _maybe.HasValue ? 1 : 0;
            bool IQueue<T>.IsFull => false;

            public void Enqueue(T item) => _maybe = item;

            public T Peek()
            {
                if (!_maybe.HasValue) throw new InvalidOperationException(Strings.QueueIsEmpty);
                return _maybe.GetValueOrDefault();
            }

            public T Dequeue()
            {
                if (!_maybe.HasValue) throw new InvalidOperationException(Strings.QueueIsEmpty);
                var maybe = Linx.Clear(ref _maybe);
                return maybe.GetValueOrDefault();
            }

            public IReadOnlyCollection<T> DequeueAll()
            {
                var maybe = Linx.Clear(ref _maybe);
                return maybe.HasValue ? new[] { maybe.GetValueOrDefault() } : LinxCollections.EmptyList<T>();
            }

            public void Clear() => _maybe = default;
        }
    }
}
