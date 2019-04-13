namespace Linx.Collections
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Priority queue implemented as a heap (ascending order).
    /// </summary>
    public sealed class PriorityQueue<T> : IQueue<T>
    {
        private readonly List<T> _heap;
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Create empty.
        /// </summary>
        /// <param name="comparer">Optional. Item comparer.</param>
        public PriorityQueue(IComparer<T> comparer = null)
        {
            _heap = new List<T>();
            _comparer = comparer ?? Comparer<T>.Default;
        }

        /// <summary>
        /// Create with initial items.
        /// </summary>
        /// <param name="initial">Initial items.</param>
        /// <param name="comparer">Optional. Item comparer.</param>
        public PriorityQueue(IEnumerable<T> initial, IComparer<T> comparer = null)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            _heap = new List<T>(initial);
            _comparer = comparer ?? Comparer<T>.Default;
            for (var i = (_heap.Count - 1) >> 1; i >= 0; i--)
                DownHeap(_heap[i], i);
        }

        /// <inheritdoc />
        public bool IsEmpty => _heap.Count == 0;

        /// <inheritdoc />
        public void Enqueue(T item)
        {
            if (_heap.Count == 0)
            {
                _heap.Add(item);
                return;
            }

            var index = (_heap.Count - 1) >> 1;
            var parent = _heap[index];
            if (_comparer.Compare(parent, item) <= 0)
            {
                _heap.Add(item);
                return;
            }
            _heap.Add(parent);

            while (true)
            {
                if (index == 0)
                    break;
                var parentIndex = (index - 1) >> 1;
                parent = _heap[parentIndex];
                if (_comparer.Compare(parent, item) <= 0)
                    break;
                _heap[index] = parent;
                index = parentIndex;
            }
            _heap[index] = item;
        }

        /// <inheritdoc />
        public T Peek() => _heap.Count > 0 ? _heap[0] : throw new InvalidOperationException(Strings.QueueIsEmpty);

        /// <inheritdoc />
        public T Dequeue()
        {
            if (_heap.Count == 0) throw new InvalidOperationException(Strings.QueueIsEmpty);

            var count = _heap.Count - 1;
            var last = _heap[count];
            _heap.RemoveAt(count);
            if (count == 0) return last;

            var first = _heap[0];
            DownHeap(last, 0);
            return first;
        }

        /// <inheritdoc />
        public void Clear() => _heap.Clear();

        private void DownHeap(T item, int index)
        {
            while (true)
            {
                var childIndex = (index + 1) << 1;
                T child;
                if (childIndex < _heap.Count)
                {
                    child = _heap[childIndex];
                    var leftIndex = childIndex - 1;
                    var left = _heap[leftIndex];
                    if (_comparer.Compare(left, child) < 0)
                    {
                        childIndex = leftIndex;
                        child = left;
                    }
                }
                else if (--childIndex < _heap.Count)
                    child = _heap[childIndex];
                else
                    break;
                if (_comparer.Compare(item, child) <= 0)
                    break;
                _heap[index] = child;
                index = childIndex;
            }
            _heap[index] = item;
        }
    }
}
