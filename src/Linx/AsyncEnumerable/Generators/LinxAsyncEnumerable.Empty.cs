namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets the empty sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>() => EmptyAsyncEnumerable<T>.Singleton;

        /// <summary>
        /// Gets the empty sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Empty<T>(T _) => EmptyAsyncEnumerable<T>.Singleton;

        private sealed class EmptyAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            public static EmptyAsyncEnumerable<T> Singleton { get; } = new EmptyAsyncEnumerable<T>();
            private EmptyAsyncEnumerable() { }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => this;

            public override string ToString() => "Empty";

            T IAsyncEnumerator<T>.Current => default;
            ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => new ValueTask<bool>(false);
            ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(Task.CompletedTask);
        }
    }
}
