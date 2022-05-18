using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Linx.Collections;

internal class LinxQueue<T>
{
    private readonly int _initialCapacity;
    private readonly int _maxCapacity;
    private T[]? _buffer;
    private int _offset, _count;

    public T[]? Buffer => _buffer;
    public int Offset => _offset;
    public int Count => _count;
    public bool IsEmpty => Count == 0;
    public bool IsFull => Count == _maxCapacity;

    /// <summary>
    /// Initialize.
    /// </summary>
    /// <param name="maxCapacity">Maximum capacity.</param>
    /// <param name="initial1">Whether to start with a buffer of size 1 (batching) rather than a default capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> must be positive.</exception>
    public LinxQueue(int maxCapacity, bool initial1)
    {
        if (maxCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maxCapacity));

        _maxCapacity = maxCapacity;
        if (initial1)
            _initialCapacity = 1;
        else
        {
            _initialCapacity = maxCapacity;
            while (_initialCapacity > 7)
                _initialCapacity >>= 1;
        }

        _buffer = null;
        _offset = _count = 0;
    }

    /// <summary>
    /// Enqueue an item.
    /// </summary>
    /// <exception cref="InvalidOperationException">Queue is full.</exception>
    public void Enqueue(T item)
    {
        if (_count == _maxCapacity) throw new InvalidOperationException(Strings.QueueIsFull);

        if (_buffer is null)
        {
            Debug.Assert(_offset == 0 && _count == 0);

            _buffer = new T[_initialCapacity];
            _buffer[0] = item;
            _count = 1;
        }
        else if (_buffer.Length < _maxCapacity)
        {
            if (_offset == 0)
                _buffer[_count++] = item;
            else
            {
                var ix = _offset - _buffer.Length + _count++;
                _buffer[ix >= 0 ? ix : ix + _buffer.Length] = item;
            }
        }
        else // _buffer is full; increase size
        {
            var s = _maxCapacity;
            while (s > 7)
            {
                var s1 = s >> 1;
                if (s1 <= _buffer.Length)
                    break;
                s = s1;
            }
            var b = new T[s];
            if (_offset == 0)
                Array.Copy(_buffer, b, _count);
            else
            {
                var c = _buffer.Length - _offset;
                Array.Copy(_buffer, _offset, b, 0, c);
                Array.Copy(_buffer, 0, b, c, _count - c);
                _offset = 0;
            }
            _buffer[_count++] = item;
        }
    }

    /// <summary>
    /// Dequeue an item.
    /// </summary>
    /// <exception cref="InvalidOperationException">Queue is empty.</exception>
    public T Dequeue()
    {
        if (_count == 0) throw new InvalidOperationException(Strings.QueueIsEmpty);
        Debug.Assert(_buffer is not null);

        var result = Linx.Clear(ref _buffer[_offset]!);
        if (--_count == 0)
        {
            _offset = 0;
            if (_buffer.Length > _initialCapacity)
                _buffer = null;
        }
        else
        {
            if (++_offset == _buffer.Length)
                _offset = 0;
        }
        return result;
    }

    public IReadOnlyCollection<T> DequeueBatch()
    {
        if (_count == 0)
            return Array.Empty<T>();

        Debug.Assert(_buffer is not null);
        IReadOnlyCollection<T> result;
        if (_offset == 0)
        {
            if (_count == _buffer.Length)
                result = _buffer;
            else
                result = new ArraySegment<T>(_buffer, 0, _count);
        }
        else
        {
            var c0 = _buffer.Length - _offset;
            var c1 = _count - c0;
            if (c1 <= 0)
                result = new ArraySegment<T>(_buffer, _offset, _count);
            else
                result = new Concat(new ArraySegment<T>(_buffer, _offset, c0), new ArraySegment<T>(_buffer, 0, c1));
        }
        Clear();
        return result;
    }

    public void Clear()
    {
        _buffer = null;
        _offset = _count = 0;
    }

    private sealed class Concat : IReadOnlyCollection<T>, ICollection<T>
    {
        private readonly ArraySegment<T> _seg0, _seg1;

        public Concat(ArraySegment<T> seg0, ArraySegment<T> seg1)
        {
            _seg0 = seg0;
            _seg1 = seg1;
            Count = _seg0.Count + _seg1.Count;
        }

        public int Count { get; init; }

        bool ICollection<T>.IsReadOnly => true;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _seg0)
                yield return item;
            foreach (var item in _seg1)
                yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array is null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex <= 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Is less than 0.");
            if (array.Length - arrayIndex < Count) throw new ArgumentException("The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array.");

            _seg0.CopyTo(array, arrayIndex);
            _seg1.CopyTo(array, arrayIndex + _seg0.Count);
        }

        bool ICollection<T>.Contains(T item) => ((ICollection<T>)_seg0).Contains(item) || ((ICollection<T>)_seg1).Contains(item);

        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        void ICollection<T>.Clear() => throw new NotSupportedException();
    }
}
