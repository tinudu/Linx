namespace Linx.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Collection extension methods.
    /// </summary>
    public static partial class LinxCollections
    {
        /// <summary>
        /// Gets a empty list singleton.
        /// </summary>
        public static IReadOnlyList<T> EmptyList<T>() => EmptyListImpl<T>.Instance;

        private sealed class EmptyListImpl<T> : IReadOnlyList<T>, IEnumerator<T>
        {
            public static IReadOnlyList<T> Instance { get; } = new EmptyListImpl<T>();
            private EmptyListImpl() { }

            int IReadOnlyCollection<T>.Count => 0;
            T IReadOnlyList<T>.this[int index] => throw new IndexOutOfRangeException();
            IEnumerator<T> IEnumerable<T>.GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;
            T IEnumerator<T>.Current => default;
            object IEnumerator.Current => default;
            bool IEnumerator.MoveNext() => false;
            void IDisposable.Dispose() { }
            void IEnumerator.Reset() { }
        }
    }
}
