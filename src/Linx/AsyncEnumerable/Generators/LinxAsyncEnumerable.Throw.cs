namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Returns an empty sequence that terminates with an exception.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
        public static IAsyncEnumerable<T> Throw<T>(Exception exception) => new ThrowIterator<T>(exception);

        /// <summary>
        /// Returns an empty sequence that terminates with an exception.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
        public static IAsyncEnumerable<T> Throw<T>(T _, Exception exception) => new ThrowIterator<T>(exception);

        /// <summary>
        /// Represents an empty sequence that terminates with an exception.
        /// </summary>
        public sealed class ThrowIterator<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private readonly ValueTask<bool> _failed;

            /// <summary>
            /// Initialize.
            /// </summary>
            /// <exception cref="ArgumentNullException"><paramref name="exception"/> is null.</exception>
            public ThrowIterator(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));
                _failed = new ValueTask<bool>(Task.FromException<bool>(exception));
            }

            IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => this;
            T IAsyncEnumerator<T>.Current => default;
            ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => _failed;
            ValueTask IAsyncDisposable.DisposeAsync() => new(Task.CompletedTask);
        }
    }
}
