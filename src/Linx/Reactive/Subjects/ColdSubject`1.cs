namespace Linx.Reactive.Subjects
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A <see cref="ISubject{T}"/> that disallowes late subscribers.
    /// </summary>
    public sealed class ColdSubject<T> : ISubject<T>
    {
        public ColdSubject() => throw new NotImplementedException();

        public ColdSubject(int capacity) => throw new NotImplementedException();

        /// <inheritdoc />
        public IAsyncEnumerable<T> Sink => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SubscribeTo(IAsyncEnumerable<T> source) => throw new NotImplementedException();
    }
}
