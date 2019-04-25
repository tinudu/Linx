namespace Linx.Collections
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Synchronized, exception free pool for values of type <typeparamref name="T"/>.
    /// </summary>
    [DebuggerNonUserCode]
    public class Pool<T>
    {
        private int _count;
        private T[] _pool = new T[4];

        /// <summary>
        /// Try to get a value from the pool.
        /// </summary>
        /// <param name="value">The value, if any.</param>
        /// <returns>Whether a value is retrieved.</returns>
        public bool TryGet(out T value)
        {
            var count = Atomic.Lock(ref _count);
            if (count == 0)
            {
                value = default;
                _count = 0;
                return false;
            }

            value = Linx.Clear(ref _pool[--count]);
            _count = count;
            return true;
        }

        /// <summary>
        /// Return the specified <paramref name="value"/> to the pool.
        /// </summary>
        public void Return(T value)
        {
            var count = Atomic.Lock(ref _count);
            if (count == _pool.Length)
                try
                {
                    var pool = new T[count << 1];
                    Array.Copy(_pool, pool, count);
                    _pool = pool;
                }
                catch
                {
                    _count = count;
                    return;
                }

            _pool[count++] = value;
            _count = count;
        }
    }
}
