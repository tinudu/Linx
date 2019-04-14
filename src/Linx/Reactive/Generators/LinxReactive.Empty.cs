namespace Linx.Reactive
{
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Gets the empty sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>() => EmptyAsyncEnumerable<T>.Singleton;

        /// <summary>
        /// Gets the empty sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>(T sample) => EmptyAsyncEnumerable<T>.Singleton;

        private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            public static EmptyAsyncEnumerable<T> Singleton { get; } = new EmptyAsyncEnumerable<T>();
            private EmptyAsyncEnumerable() { }

            IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token) => this;

            T IAsyncEnumerator<T>.Current => default;
            ICoAwaiter<bool> IAsyncEnumerator<T>.MoveNextAsync(bool continueOnCapturedContext) => CoAwaiter.False;
            Task IAsyncEnumerator<T>.DisposeAsync() => Task.CompletedTask;
        }
    }
}
