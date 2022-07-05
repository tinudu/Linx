using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxConnectable
{
    /// <summary>
    /// Multiple consumers sharing a subject.
    /// </summary>
    public static ValueTask MultiConsume<T>(this IConnectable<T> source, IEnumerable<ConsumerDelegate<T>> consumers, CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (consumers == null) throw new ArgumentNullException(nameof(consumers));

        return MultiConsumer<T>.MultiConsume(source, consumers, token);
    }

    /// <summary>
    /// Multiple consumers sharing a subject.
    /// </summary>
    public static ValueTask MultiConsume<T>(this IConnectable<T> source, CancellationToken token, params ConsumerDelegate<T>[] consumers)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (consumers == null) throw new ArgumentNullException(nameof(consumers));

        return MultiConsumer<T>.MultiConsume(source, consumers, token);
    }

    private sealed class MultiConsumer<T>
    {
        public static async ValueTask MultiConsume(IConnectable<T> source, IEnumerable<ConsumerDelegate<T>> consumers, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiConsumer<T>(source.CreateSubject(), token);
            foreach (var consumer in consumers)
            {
                multi.Start(consumer);
                if (multi._error is not null)
                    break;
            }
            multi.Connect();
            await multi._atmbWaitAll.Task.ConfigureAwait(false);
        }

        private readonly ISubject<T> _subject;
        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private AsyncTaskMethodBuilder _atmbWaitAll = AsyncTaskMethodBuilder.Create();
        private int _count;
        private int _state; // 0: initial, 1: connected
        private Exception? _error;

        private MultiConsumer(ISubject<T> subject, CancellationToken token)
        {
            _subject = subject;
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Start(ConsumerDelegate<T> consumer)
        {
            Atomic.Lock(ref _state);
            Debug.Assert(_state == ~0);
            if (_error is null)
            {
                _count++;
                _state = 0;
                try { await consumer(_subject.AsyncEnumerable, _cts.Token).ConfigureAwait(false); }
                catch (Exception ex) { OnError(ex); }
                finally { OnCompleted(); }
            }
            else
                _state = 0;
        }

        private void Connect()
        {
            Atomic.Lock(ref _state);
            Debug.Assert(_state == ~0);
            if (_count > 0)
            {
                _state = 1;
                _subject.Connect();
            }
            else if (_error is null)
            {
                _state = 1;
                _ctr.Dispose();
                _atmbWaitAll.SetResult();
                _cts.Cancel();
            }
            else
            {
                _state = 1;
                _atmbWaitAll.SetException(Linx.Clear(ref _error));
            }
        }

        private void OnError(Exception error)
        {
            var state = Atomic.Lock(ref _state);
            if (_error is null && (state == 0 || state == 1 && _count > 0))
            {
                _error = error;
                _state = state;
                _ctr.Dispose();
                _cts.Cancel();
            }
            else
                _state = state;
        }

        private void OnCompleted()
        {
            var state = Atomic.Lock(ref _state);
            Debug.Assert(_count > 0);
            if (--_count > 0 || state == 0)
                _state = 0;
            else if (_error is null)
            {
                _state = 1;
                _ctr.Dispose();
                _atmbWaitAll.SetResult();
                _cts.Cancel();
            }
            else
            {
                _state = 1;
                _atmbWaitAll.SetException(Linx.Clear(ref _error));
            }
        }
    }
}
