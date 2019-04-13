namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Coroutines;
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

            var state = 0;
            var eh = ErrorHandler.Init();
            if (token.CanBeCanceled) eh.ExternalRegistration = token.Register(() =>
            {
                var error = new OperationCanceledException(token);
                var s = Atomic.Lock(ref state);
                eh.SetExternalError(error);
                state = 1;
                if (s == 0) eh.Cancel();
            });

            void HandleInternalError(Exception error)
            {
                var s = Atomic.Lock(ref state);
                eh.SetInternalError(error);
                state = 1;
                if (s == 0) eh.Cancel();
            }

            var subj = new ColdSubject<TSource>();
            var a1 = Aggregate(aggregator1);
            var a2 = Aggregate(aggregator2);
            var subscription = Subscribe();
            await a1.ConfigureAwait(false);
            await a2.ConfigureAwait(false);
            await subscription.ConfigureAwait(false);
            eh.ThrowIfError();
            eh.Cancel();
            return resultSelector(a1.Result, a2.Result);

            async Task<TAggregate> Aggregate<TAggregate>(AggregatorDelegate<TSource, TAggregate> aggregator)
            {
                try { return await aggregator(subj.Sink, eh.InternalToken).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                    return default;
                }
            }

            async Task Subscribe()
            {
                try { await subj.SubscribeTo(source).ConfigureAwait(false); }
                catch (Exception ex) { HandleInternalError(ex); }
            }
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
    }
}
