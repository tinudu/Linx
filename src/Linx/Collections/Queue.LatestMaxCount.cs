namespace Linx.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    partial class Queue
    {
        private sealed class LatestMaxCountQueue<T> : IQueue<T>
        {
            private readonly int _maxCount;
            private Queue<T> _queue;

            public LatestMaxCountQueue(int maxCount)
            {
                Debug.Assert(maxCount >= 2);
                _maxCount = maxCount;
            }

            public int Count => _queue?.Count ?? 0;

            bool IQueue<T>.IsFull => false;

            public void Enqueue(T item)
            {
                if (_queue == null)
                    _queue = new Queue<T>();
                else if (_queue.Count == _maxCount)
                    _queue.Dequeue();
                _queue.Enqueue(item);
            }

            public T Peek()
            {
                if (_queue == null || _queue.Count == 0) throw new InvalidOperationException(Strings.QueueIsEmpty);
                return _queue.Peek();
            }

            public T Dequeue()
            {
                if (_queue == null || _queue.Count == 0) throw new InvalidOperationException(Strings.QueueIsEmpty);
                return _queue.Dequeue();
            }

            public IReadOnlyCollection<T> DequeueAll()
            {
                if (_queue == null || _queue.Count == 0) return LinxCollections.EmptyList<T>();
                return Linx.Clear(ref _queue);
            }

            public void Clear()
            {
                if(_queue==null)return;
                try
                {
                    _queue.Clear();
                    _queue.TrimExcess();
                }
                catch { _queue = null; }
            }
        }
    }
}
