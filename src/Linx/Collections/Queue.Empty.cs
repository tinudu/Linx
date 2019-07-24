namespace Linx.Collections
{
    using System;
    using System.Collections.Generic;

    partial class Queue
    {
        private sealed class EmptyQueue<T> : IQueue<T>
        {
            public static EmptyQueue<T> Instance { get; } = new EmptyQueue<T>();
            private EmptyQueue() { }

            public int Count => 0;
            bool IQueue<T>.IsFull => false;

            public void Enqueue(T item) { }

            public T Peek() => throw new InvalidOperationException(Strings.QueueIsEmpty);
            public T Dequeue() => throw new InvalidOperationException(Strings.QueueIsEmpty);
            public IReadOnlyCollection<T> DequeueAll() => LinxCollections.EmptyList<T>();
            public void Clear() { }
        }
    }
}