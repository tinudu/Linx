namespace Linx.Reactive.Subjects
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// A <see cref="ISubject{T}"/> that disallowes late subscribers.
    /// </summary>
    public sealed class ColdSubject<T> : ISubject<T>
    {
        /// <inheritdoc />
        public IAsyncEnumerable<T> Sink => throw new NotImplementedException();

        /// <inheritdoc />
        public Task SubscribeTo(IAsyncEnumerable<T> source) => throw new NotImplementedException();
    }
}
