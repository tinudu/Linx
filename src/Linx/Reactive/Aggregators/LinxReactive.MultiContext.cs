namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Subjects;

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

            var ctx = new MultiContext<TSource>(token);
            var t1 = ctx.Aggregate(aggregator1);
            var t2 = ctx.Aggregate(aggregator2);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            await t1.ConfigureAwait(false);
            await t2.ConfigureAwait(false);
            ctx.Complete();
            return resultSelector(t1.Result, t2.Result);
        }

        /// <summary>
        /// Build multiple aggregates in one run.
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
        /// Build multiple aggregates in one run.
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

            var ctx = new MultiContext<TSource>(token);
            var t1 = ctx.Consume(consumer1);
            var t2 = ctx.Consume(consumer2);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            await t1.ConfigureAwait(false);
            await t2.ConfigureAwait(false);
            ctx.Complete();
        }

        private sealed class MultiContext<TSource>
        {
            private int _state;
            private ErrorHandler _eh = ErrorHandler.Init();
            private readonly ISubject<TSource> _subj = new ColdSubject<TSource>();

            public MultiContext(CancellationToken token)
            {
                if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() =>
                {
                    if (Atomic.TestAndSet(ref _state, 0, 1) != 0) return;
                    _eh.SetExternalError(new OperationCanceledException(token));
                    _eh.Cancel();
                });
            }

            public async Task<TAggregate> Aggregate<TAggregate>(AggregatorDelegate<TSource, TAggregate> aggregator)
            {
                try { return await aggregator(_subj.Sink, _eh.InternalToken).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                    return default;
                }
            }

            public async Task Consume(ConsumerDelegate<TSource> consumer)
            {
                try { await consumer(_subj.Sink, _eh.InternalToken).ConfigureAwait(false); }
                catch (Exception ex) { HandleInternalError(ex); }
            }

            public async Task SubscribeTo(IAsyncEnumerable<TSource> src)
            {
                try { await _subj.SubscribeTo(src).ConfigureAwait(false); }
                catch (Exception ex) { HandleInternalError(ex); }
            }

            public void Complete()
            {
                if (Atomic.TestAndSet(ref _state, 0, 1) == 0) _eh.Cancel();
                _eh.ThrowIfError();
            }

            private void HandleInternalError(Exception error)
            {
                var s = Atomic.Lock(ref _state);
                _eh.SetInternalError(error);
                _state = 1;
                if (s == 0) _eh.Cancel();
            }
        }
    }
}
