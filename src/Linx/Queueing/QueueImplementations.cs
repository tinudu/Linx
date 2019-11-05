namespace Linx.Queueing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal sealed class BufferNoneQueue<T> : IQueue<T>
    {
        public static BufferNoneQueue<T> Instance { get; } = new BufferNoneQueue<T>();
        private BufferNoneQueue() { }

        bool IQueue<T>.IsEmpty => true;
        bool IQueue<T>.IsFull => true;
        void IQueue<T>.Enqueue(T item) => throw new BufferOverflowException();
        T IQueue<T>.Dequeue() => throw new InvalidOperationException("Queue is empty.");
        void IQueue<T>.Clear() { }
        void IQueue<T>.TrimExcess() { }
    }

    internal sealed class BufferMaxQueue<T> : Queue<T>, IQueue<T>
    {
        private readonly int _maxCount;

        public BufferMaxQueue(int maxCount)
        {
            Debug.Assert(maxCount > 0 && maxCount < int.MaxValue, "use a specialized implementation");
            _maxCount = maxCount;
        }

        public bool IsEmpty => Count == 0;
        public bool IsFull => Count == _maxCount;

        void IQueue<T>.Enqueue(T item)
        {
            if (Count == _maxCount) throw new BufferOverflowException();
            Enqueue(item);
        }
    }

    internal sealed class BufferAllQueue<T> : Queue<T>, IQueue<T>
    {
        public bool IsEmpty => Count == 0;
        bool IQueue<T>.IsFull => false;
    }

    internal sealed class NextQueue<T> : IQueue<T>
    {
        public static NextQueue<T> Instance { get; } = new NextQueue<T>();
        private NextQueue() { }

        bool IQueue<T>.IsEmpty => true;
        bool IQueue<T>.IsFull => false;
        void IQueue<T>.Enqueue(T item) { }
        T IQueue<T>.Dequeue() => throw new InvalidOperationException("Queue is empty.");
        void IQueue<T>.Clear() { }
        void IQueue<T>.TrimExcess() { }
    }

    internal sealed class LatestOneQueue<T> : IQueue<T>
    {
        private (bool HasValue, T Single) _item;

        public bool IsEmpty => !_item.HasValue;
        bool IQueue<T>.IsFull => false;
        void IQueue<T>.Enqueue(T item) => _item = (true, item);
        public T Dequeue() => _item.HasValue ? Linx.Clear(ref _item).Single : throw new InvalidOperationException("Queue is empty.");
        public void Clear() => _item = default;
        void IQueue<T>.TrimExcess() { }
    }

    internal sealed class LatestMaxQueue<T> : Queue<T>, IQueue<T>
    {
        private readonly int _maxCount;

        public LatestMaxQueue(int maxCount)
        {
            Debug.Assert(maxCount >= 2 && maxCount < int.MaxValue, "use a specialized implementation");
            _maxCount = maxCount;
        }

        public bool IsEmpty => Count == 0;
        bool IQueue<T>.IsFull => false;
        void IQueue<T>.Enqueue(T item)
        {
            while (Count >= _maxCount)
                Dequeue();
            Enqueue(item);
        }
    }
}
