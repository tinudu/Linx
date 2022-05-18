using System;
using System.Collections.Generic;

namespace Linx.Collections;

/// <summary>
/// Priority queue implemented as a heap (ascending order).
/// </summary>
public sealed class PriorityQueue<T>
{
    private readonly List<T> _heap;
    private readonly IComparer<T> _comparer;

    /// <summary>
    /// Create empty.
    /// </summary>
    /// <param name="comparer">Optional. Item comparer.</param>
    public PriorityQueue(IComparer<T>? comparer = null)
    {
        _heap = new List<T>();
        _comparer = comparer ?? Comparer<T>.Default;
    }

    /// <summary>
    /// Create with initial items.
    /// </summary>
    /// <param name="initial">Initial items.</param>
    /// <param name="comparer">Optional. Item comparer.</param>
    public PriorityQueue(IEnumerable<T> initial, IComparer<T>? comparer = null)
    {
        if (initial == null) throw new ArgumentNullException(nameof(initial));

        _heap = new List<T>(initial);
        _comparer = comparer ?? Comparer<T>.Default;
        for (var i = (_heap.Count - 1) >> 1; i >= 0; i--)
            DownHeap(_heap[i], i);
    }

    /// <summary>
    /// Returns the number of items in the queue.
    /// </summary>
    public int Count => _heap.Count;

    /// <summary>
    /// Adds an item to the queue.
    /// </summary>
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

    /// <summary>
    /// Removes and returns the item at the beginning of the queue.
    /// </summary>
    /// <exception cref="OperationCanceledException">The queue is empty.</exception>
    public T Peek() => _heap.Count > 0 ? _heap[0] : throw new InvalidOperationException(Strings.QueueIsEmpty);

    /// <summary>
    /// Returns the item at the beginning of the queue without removing it.
    /// </summary>
    /// <exception cref="OperationCanceledException">The queue is empty.</exception>
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

    /// <summary>
    /// Not supported.
    /// </summary>
    /// <exception cref="NotSupportedException"/>
    public IReadOnlyCollection<T> DequeueAll() => throw new NotSupportedException();

    /// <summary>
    /// Removes all items from the queue.
    /// </summary>
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
