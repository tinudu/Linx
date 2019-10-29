namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static Task MultiConsume<T>(this IAsyncEnumerable<T> source, IEnumerable<ConsumerDelegate<T>> consumers, CancellationToken token)
            => MultiConsumer<T>.Consume(source, consumers, token);

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static Task MultiConsume<T>(this IAsyncEnumerable<T> source, CancellationToken token, params ConsumerDelegate<T>[] consumers)
            => MultiConsumer<T>.Consume(source, consumers, token);

        private sealed class MultiConsumer<T>
        {
            public static async Task Consume(
                IAsyncEnumerable<T> source,
                IEnumerable<ConsumerDelegate<T>> consumers,
                CancellationToken token)
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                if (consumers == null) throw new ArgumentNullException(nameof(consumers));
                token.ThrowIfCancellationRequested();

                var collection = consumers as IReadOnlyCollection<ConsumerDelegate<T>> ?? consumers.ToList();
                switch (collection.Count)
                {
                    case 0: return;
                    case 1:
                        await collection.Single()(source, token).ConfigureAwait(false);
                        break;
                    default:
                        var multi = new MultiConsumer<T>(collection.Count, token);
                        var connectable = source.Connectable(out var connect);
                        foreach (var consumer in collection)
                            // ReSharper disable once PossibleMultipleEnumeration
                            multi.Subscribe(connectable, consumer);
                        connect();
                        await multi._atmbWhenAll.Task.ConfigureAwait(false);
                        break;
                }
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private readonly CancellationTokenRegistration _ctr;
            private int _active;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();

            private MultiConsumer(int active, CancellationToken token)
            {
                _active = active;
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe(IAsyncEnumerable<T> source, ConsumerDelegate<T> consumer)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    await consumer(source, _cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }

            private void OnError(Exception error)
            {
                var active = Atomic.Lock(ref _active);
                if (_error != null || active == 0)
                {
                    _active = active;
                    return;
                }

                _error = error;
                _active = active;
                _ctr.Dispose();
                try { _cts.Cancel(); } catch { /**/ }
            }

            private void OnCompleted()
            {
                var active = Atomic.Lock(ref _active);
                Debug.Assert(active > 0);
                _active = --active;
                if (active > 0) return;

                if (_error == null)
                {
                    _ctr.Dispose();
                    try { _cts.Cancel(); } catch { /**/ }
                    _atmbWhenAll.SetResult();
                }
                else
                    _atmbWhenAll.SetException(_error);
            }
        }

    }
}
