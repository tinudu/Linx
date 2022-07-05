using System;
using System.Collections.Generic;
using Linx.AsyncEnumerable.Subjects;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets a <see cref="IConnectable{T}"/> that uses a <see cref="ColdSubject{T}"/>.
    /// </summary>
    public static IConnectable<T> Cold<T>(this IAsyncEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return new AnonymousConnectable<T>(() => new ColdSubject<T>(source));
    }

    /// <summary>
    /// Gets a <see cref="IConnectable{T}"/> that uses a <see cref="ColdSubject{T}"/>.
    /// </summary>
    public static IConnectable<T> Cold<T>(this IEnumerable<T> source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return new AnonymousConnectable<T>(() => new ColdSubject<T>(source.ToAsync()));
    }

    private sealed class AnonymousConnectable<T> : IConnectable<T>
    {
        private readonly Func<ISubject<T>> _getSubject;

        public AnonymousConnectable(Func<ISubject<T>> getSubject) => _getSubject = getSubject;

        public ISubject<T> CreateSubject() => _getSubject();
    }
}
