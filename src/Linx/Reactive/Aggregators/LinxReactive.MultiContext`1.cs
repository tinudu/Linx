namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;

    partial class LinxReactive
    {
        /// <summary>
        /// Build multiple aggregates in one run.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            Func<TAggregate1, TAggregate2, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(2, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking - intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            Func<TAggregate1, TAggregate2, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(2, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

        private sealed class MultiContext<T> : IAsyncEnumerable<T>
        {
            private const int _sInitial = 0;
            private const int _sSubscribed = 1;
            private const int _sCanceling = 2;

            private readonly List<Enumerator> _enumerators;
            private int _state, _active, _index;
            private ErrorHandler _eh = ErrorHandler.Init();
            private AsyncTaskMethodBuilder _atmbDone = default;

            public MultiContext(int capacity, CancellationToken token)
            {
                _enumerators = new List<Enumerator>(capacity);
                if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() => Cancel(new OperationCanceledException(token)));
            }

            public async Task<TAggregate> Aggregate<TAggregate>(AggregatorDelegate<T, TAggregate> aggregator)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                _active++;
                _state = _sInitial;

                TAggregate result;
                Exception error;
                try
                {
                    result = await aggregator(this, _eh.InternalToken).ConfigureAwait(false);
                    error = null;
                }
                catch (Exception ex)
                {
                    result = default;
                    error = ex;
                }

                HandleInternalError(error);
                return result;
            }

            public async void Consume(ConsumerDelegate<T> consumer)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                _active++;
                _state = _sInitial;

                Exception error;
                try
                {
                    await consumer(this, _eh.InternalToken).ConfigureAwait(false);
                    error = null;
                }
                catch (Exception ex) { error = ex; }

                HandleInternalError(error);
            }

            public async Task SubscribeTo(IAsyncEnumerable<T> src)
            {
                var state = Atomic.Lock(ref _state);
                if (state != _sInitial)
                {
                    _state = state;
                    throw new InvalidOperationException();
                }

                if (_enumerators.Count == 0)
                    _state = _sSubscribed;
                else
                {
                    _active++;
                    _state = _sSubscribed;
                    Exception error;
                    try
                    {
                        var ae = src.GetAsyncEnumerator(_eh.InternalToken);
                        try
                        {
                            while (await ae.MoveNextAsync())
                            {
                                var current = ae.Current;
                                Debug.Assert(_index == 0);
                                bool @break;
                                while (true)
                                {
                                    state = Atomic.Lock(ref _state);
                                    if (_state != _sSubscribed)
                                    {
                                        @break = true;
                                        _state = state;
                                        break;
                                    }

                                    if (_index < _enumerators.Count)
                                    {
                                        var e = _enumerators[_index++];
                                        _state = _sSubscribed;
                                        await e.OnNext(current);
                                    }
                                    else
                                    {
                                        if (_index == 0)
                                            @break = true;
                                        else
                                        {
                                            @break = false;
                                            _index = 0;
                                        }
                                        _state = _sSubscribed;
                                        break;
                                    }
                                }

                                if (@break) break;
                            }
                        }
                        finally { await ae.DisposeAsync().ConfigureAwait(false); }
                        error = null;
                    }
                    catch (Exception ex) { error = ex; }

                }
            }

            private void HandleInternalError(Exception error)
            {
                var s = Atomic.Lock(ref _state);
                _eh.SetInternalError(error);
                _state = 1;
                if (s == 0) _eh.Cancel();
            }

            IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken token) => new Enumerator(this, token);

            private void Cancel(Exception error)
            {
                if (Atomic.TestAndSet(ref _state, 0, 1) != 0) return;
                _eh.SetExternalError(error);
                _eh.Cancel();
            }

            private sealed class Enumerator : IAsyncEnumerator<T>
            {
                private readonly MultiContext<T> _context;
                public readonly CoAwaiterCompleter<bool> AcPulling = CoAwaiterCompleter<bool>.Init();
                public readonly T Current;
                public Exception Error;

                public Enumerator(MultiContext<T> context, CancellationToken token)
                {
                    _context = context;
                }

                public ICoAwaiter OnNext(T value) => CoAwaiter.Completed;

                T IAsyncEnumerator<T>.Current => Current;

                ICoAwaiter<bool> IAsyncEnumerator<T>.MoveNextAsync(bool continueOnCapturedContext)
                {
                    throw new NotImplementedException();
                }

                Task IAsyncEnumerator<T>.DisposeAsync()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
