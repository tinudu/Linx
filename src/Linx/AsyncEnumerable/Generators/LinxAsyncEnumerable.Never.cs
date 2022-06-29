using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets a <see cref="IAsyncEnumerable{T}"/> that completes only when the token is canceled or when it's disposed.
    /// </summary>
    public static IAsyncEnumerable<T> Never<T>() => NeverAsyncEnumerable<T>.Singleton;

    /// <summary>
    /// Gets a <see cref="IAsyncEnumerable{T}"/> that completes only when the token is canceled or when it's disposed.
    /// </summary>
    public static IAsyncEnumerable<T> Never<T>(T _) => NeverAsyncEnumerable<T>.Singleton;

    private sealed class NeverAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public static NeverAsyncEnumerable<T> Singleton { get; } = new NeverAsyncEnumerable<T>();
        private NeverAsyncEnumerable() { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token) => new Enumerator(token);

        private sealed class Enumerator : IAsyncEnumerator<T>
        {
            private AsyncTaskMethodBuilder<bool> _atmbMoveNext;
            private readonly CancellationTokenRegistration _ctr;
            private int _state;

            public Enumerator(CancellationToken token)
            {
                if (token.CanBeCanceled)
                    _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
            }

            public T Current => default!;

            public ValueTask<bool> MoveNextAsync() => new(_atmbMoveNext.Task);

            public ValueTask DisposeAsync()
            {
                SetFinal(AsyncEnumeratorDisposedException.Instance);
                return new(Task.CompletedTask);
            }

            private void SetFinal(Exception error)
            {
                if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
                    return;
                _ctr.Dispose();
                _atmbMoveNext.SetException(error);
            }
        }
    }
}
