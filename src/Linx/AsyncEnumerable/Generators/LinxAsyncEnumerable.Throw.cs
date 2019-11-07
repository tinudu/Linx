namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns a sequence that terminates with an exception.
        /// </summary>
        public static IAsyncEnumerable<T> Throw<T>(Exception exception) => new ThrowIterator<T>(exception);

        /// <summary>
        /// Returns a sequence that terminates with an exception.
        /// </summary>
        public static IAsyncEnumerable<T> Throw<T>(T _, Exception exception) => new ThrowIterator<T>(exception);

        private sealed class ThrowIterator<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private readonly ValueTask<bool> _failed;

            public ThrowIterator(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));
                _failed = new ValueTask<bool>(Task.FromException<bool>(exception));
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken) => this;
            T IAsyncEnumerator<T>.Current => default;
            ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => _failed;
            ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(Task.CompletedTask);

            public override string ToString() => "Throw";
        }
    }
}
