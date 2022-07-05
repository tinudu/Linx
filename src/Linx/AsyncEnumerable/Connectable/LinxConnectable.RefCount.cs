using System;
using System.Collections.Generic;
using System.Threading;

namespace Linx.AsyncEnumerable;

partial class LinxConnectable
{
    /// <summary>
    /// Gets the <see cref="ISubject{T}.AsyncEnumerable"/> of a connected <see cref="ISubject{T}"/>.
    /// </summary>
    public static IAsyncEnumerable<T> RefCount<T>(this IConnectable<T> connectable)
    {
        if (connectable is null) throw new ArgumentNullException(nameof(connectable));
        return new RefCountEnumerable<T>(connectable);
    }

    private sealed class RefCountEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly object _gate = new();
        private readonly IConnectable<T> _connectable;
        private ISubject<T>? _subject;

        public RefCountEnumerable(IConnectable<T> connectable) => _connectable = connectable;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
        {
            lock (_gate)
            {
                if (_subject is not null)
                    try { return _subject.AsyncEnumerable.GetAsyncEnumerator(token); }
                    catch (SubjectDisposedException) { _subject = null; }

                _subject = _connectable.CreateSubject();
                var enumerator = _subject.AsyncEnumerable.GetAsyncEnumerator(token);
                _subject.Connect();
                return enumerator;
            }
        }
    }
}
