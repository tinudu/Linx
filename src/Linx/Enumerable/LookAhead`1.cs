namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// A wrapper around a <see cref="IEnumerator{T}"/> that looks ahead one item.
    /// </summary>
    [DebuggerNonUserCode]
    public sealed class LookAhead<T> : IDisposable
    {
        private IEnumerator<T> _enumerator;

        /// <summary>
        /// Gets whether there is a next item.
        /// </summary>
        public bool HasNext { get; private set; }

        /// <summary>
        /// Gets the next item (if <see cref="HasNext"/> is true).
        /// </summary>
        public T Next { get; private set; }

        /// <summary>
        /// Initialize wit a <see cref="IEnumerable{T}"/>.
        /// </summary>
        public LookAhead(IEnumerable<T> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var e = source.GetEnumerator();
            try
            {
                if (e.MoveNext())
                {
                    Next = e.Current;
                    HasNext = true;
                    _enumerator = e;
                }
                else
                    e.Dispose();
            }
            catch
            {
                e.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Advance to the next item.
        /// </summary>
        public bool MoveNext()
        {
            if (!HasNext) return false;
            try
            {
                if (_enumerator.MoveNext())
                {
                    Next = _enumerator.Current;
                    return true;
                }
                Dispose();
                return false;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!HasNext) return;
            HasNext = false;
            Next = default;
            _enumerator.Dispose();
            _enumerator = null;
        }
    }
}
