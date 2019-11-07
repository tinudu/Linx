namespace Linx.Queueing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    internal sealed class ZeroQueue<T> : IQueue<T>
    {
        public static ZeroQueue<T> Instance { get; } = new ZeroQueue<T>();
        private ZeroQueue() { }

        bool IQueue<T>.IsEmpty => true;
        bool IQueue<T>.IsFull => true;
        void IQueue<T>.Enqueue(T item) => throw new InvalidOperationException();
        T IQueue<T>.Dequeue() => throw new InvalidOperationException("Queue is empty.");
        void IQueue<T>.Clear() { }
        void IQueue<T>.TrimExcess() { }
    }

    internal sealed class OneQueue<T> : IQueue<T>
    {
        private (bool HasValue, T Value) _q;

        public bool IsEmpty => !_q.HasValue;
        public bool IsFull => _q.HasValue;

        public void Enqueue(T item)
        {
            if (_q.HasValue) throw new InvalidOperationException();
            _q = (true, item);
        }

        public T Dequeue()
        {
            if (!_q.HasValue) throw new InvalidOperationException();
            return Linx.Clear(ref _q).Value;
        }

        public void Clear() => _q = default;
        void IQueue<T>.TrimExcess() { }
    }

    internal sealed class AllQueue<T> : Queue<T>, IQueue<T>
    {
        public bool IsEmpty => Count == 0;
        bool IQueue<T>.IsFull => false;
    }

    internal sealed class MaxQueue<T> : Queue<T>, IQueue<T>
    {
        private readonly int _maxCount;

        public MaxQueue(int maxCount)
        {
            Debug.Assert(maxCount > 0 && maxCount < int.MaxValue, "use a specialized implementation");
            _maxCount = maxCount;
        }

        public bool IsEmpty => Count == 0;
        public bool IsFull => Count == _maxCount;

        void IQueue<T>.Enqueue(T item)
        {
            if (Count == _maxCount) throw new InvalidOperationException();
            Enqueue(item);
        }
    }
}
