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
        public static IAsyncEnumerableObs<T> Empty<T>() => EmptyAsyncEnumerable<T>.Singleton;

        /// <summary>
        /// Gets the empty sequence.
        /// </summary>
        public static IAsyncEnumerableObs<T> Empty<T>(T sample) => EmptyAsyncEnumerable<T>.Singleton;

        private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerableObs<T>, IAsyncEnumeratorObs<T>
        {
            public static EmptyAsyncEnumerable<T> Singleton { get; } = new EmptyAsyncEnumerable<T>();
            private EmptyAsyncEnumerable() { }

            IAsyncEnumeratorObs<T> IAsyncEnumerableObs<T>.GetAsyncEnumerator(CancellationToken token) => this;

            T IAsyncEnumeratorObs<T>.Current => default;
            ICoAwaiter<bool> IAsyncEnumeratorObs<T>.MoveNextAsync(bool continueOnCapturedContext) => CoAwaiter.False;
            Task IAsyncEnumeratorObs<T>.DisposeAsync() => Task.CompletedTask;
        }
    }
}
