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
        public static IAsyncEnumerable<T> Throw<T>(Exception exception) => new ThrowEnuemrable<T>(exception);

        /// <summary>
        /// Returns a sequence that terminates with an exception.
        /// </summary>
        public static IAsyncEnumerable<T> Throw<T>(T sample, Exception exception) => new ThrowEnuemrable<T>(exception);

        private sealed class ThrowEnuemrable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private readonly Task<bool> _failed;

            public ThrowEnuemrable(Exception exception)
            {
                if (exception == null) throw new ArgumentNullException(nameof(exception));
                _failed = Task.FromException<bool>(exception);
            }

            IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken) => this;
            T IAsyncEnumerator<T>.Current => default;
            ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync() => new ValueTask<bool>(_failed);
            ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(Task.CompletedTask);

            public override string ToString() => "Throw";
        }
    }
}
