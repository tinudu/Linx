namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Multiple aggregators sharing a subscription.
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
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
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

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            Func<TAggregate1, TAggregate2, TAggregate3, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(3, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            var a3 = ctx.Aggregate(aggregator3);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result, a3.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            Func<TAggregate1, TAggregate2, TAggregate3, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, aggregator3, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            ConsumerDelegate<TSource> consumer3,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            if (consumer3 == null) throw new ArgumentNullException(nameof(consumer3));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(3, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            ctx.Consume(consumer3);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
            if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(4, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            var a3 = ctx.Aggregate(aggregator3);
            var a4 = ctx.Aggregate(aggregator4);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result, a3.Result, a4.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            ConsumerDelegate<TSource> consumer3,
            ConsumerDelegate<TSource> consumer4,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            if (consumer3 == null) throw new ArgumentNullException(nameof(consumer3));
            if (consumer4 == null) throw new ArgumentNullException(nameof(consumer4));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(4, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            ctx.Consume(consumer3);
            ctx.Consume(consumer4);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
            if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
            if (aggregator5 == null) throw new ArgumentNullException(nameof(aggregator5));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(5, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            var a3 = ctx.Aggregate(aggregator3);
            var a4 = ctx.Aggregate(aggregator4);
            var a5 = ctx.Aggregate(aggregator5);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result, a3.Result, a4.Result, a5.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            ConsumerDelegate<TSource> consumer3,
            ConsumerDelegate<TSource> consumer4,
            ConsumerDelegate<TSource> consumer5,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            if (consumer3 == null) throw new ArgumentNullException(nameof(consumer3));
            if (consumer4 == null) throw new ArgumentNullException(nameof(consumer4));
            if (consumer5 == null) throw new ArgumentNullException(nameof(consumer5));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(5, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            ctx.Consume(consumer3);
            ctx.Consume(consumer4);
            ctx.Consume(consumer5);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            AggregatorDelegate<TSource, TAggregate6> aggregator6,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
            if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
            if (aggregator5 == null) throw new ArgumentNullException(nameof(aggregator5));
            if (aggregator6 == null) throw new ArgumentNullException(nameof(aggregator6));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(6, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            var a3 = ctx.Aggregate(aggregator3);
            var a4 = ctx.Aggregate(aggregator4);
            var a5 = ctx.Aggregate(aggregator5);
            var a6 = ctx.Aggregate(aggregator6);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result, a3.Result, a4.Result, a5.Result, a6.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            AggregatorDelegate<TSource, TAggregate6> aggregator6,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            ConsumerDelegate<TSource> consumer3,
            ConsumerDelegate<TSource> consumer4,
            ConsumerDelegate<TSource> consumer5,
            ConsumerDelegate<TSource> consumer6,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            if (consumer3 == null) throw new ArgumentNullException(nameof(consumer3));
            if (consumer4 == null) throw new ArgumentNullException(nameof(consumer4));
            if (consumer5 == null) throw new ArgumentNullException(nameof(consumer5));
            if (consumer6 == null) throw new ArgumentNullException(nameof(consumer6));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(6, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            ctx.Consume(consumer3);
            ctx.Consume(consumer4);
            ctx.Consume(consumer5);
            ctx.Consume(consumer6);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            AggregatorDelegate<TSource, TAggregate6> aggregator6,
            AggregatorDelegate<TSource, TAggregate7> aggregator7,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
            if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
            if (aggregator5 == null) throw new ArgumentNullException(nameof(aggregator5));
            if (aggregator6 == null) throw new ArgumentNullException(nameof(aggregator6));
            if (aggregator7 == null) throw new ArgumentNullException(nameof(aggregator7));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(7, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            var a3 = ctx.Aggregate(aggregator3);
            var a4 = ctx.Aggregate(aggregator4);
            var a5 = ctx.Aggregate(aggregator5);
            var a6 = ctx.Aggregate(aggregator6);
            var a7 = ctx.Aggregate(aggregator7);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result, a3.Result, a4.Result, a5.Result, a6.Result, a7.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            AggregatorDelegate<TSource, TAggregate6> aggregator6,
            AggregatorDelegate<TSource, TAggregate7> aggregator7,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            ConsumerDelegate<TSource> consumer3,
            ConsumerDelegate<TSource> consumer4,
            ConsumerDelegate<TSource> consumer5,
            ConsumerDelegate<TSource> consumer6,
            ConsumerDelegate<TSource> consumer7,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            if (consumer3 == null) throw new ArgumentNullException(nameof(consumer3));
            if (consumer4 == null) throw new ArgumentNullException(nameof(consumer4));
            if (consumer5 == null) throw new ArgumentNullException(nameof(consumer5));
            if (consumer6 == null) throw new ArgumentNullException(nameof(consumer6));
            if (consumer7 == null) throw new ArgumentNullException(nameof(consumer7));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(7, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            ctx.Consume(consumer3);
            ctx.Consume(consumer4);
            ctx.Consume(consumer5);
            ctx.Consume(consumer6);
            ctx.Consume(consumer7);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        public static async Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(
            this IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            AggregatorDelegate<TSource, TAggregate6> aggregator6,
            AggregatorDelegate<TSource, TAggregate7> aggregator7,
            AggregatorDelegate<TSource, TAggregate8> aggregator8,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
            if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
            if (aggregator5 == null) throw new ArgumentNullException(nameof(aggregator5));
            if (aggregator6 == null) throw new ArgumentNullException(nameof(aggregator6));
            if (aggregator7 == null) throw new ArgumentNullException(nameof(aggregator7));
            if (aggregator8 == null) throw new ArgumentNullException(nameof(aggregator8));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(8, token);
            var a1 = ctx.Aggregate(aggregator1);
            var a2 = ctx.Aggregate(aggregator2);
            var a3 = ctx.Aggregate(aggregator3);
            var a4 = ctx.Aggregate(aggregator4);
            var a5 = ctx.Aggregate(aggregator5);
            var a6 = ctx.Aggregate(aggregator6);
            var a7 = ctx.Aggregate(aggregator7);
            var a8 = ctx.Aggregate(aggregator8);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
            return resultSelector(a1.Result, a2.Result, a3.Result, a4.Result, a5.Result, a6.Result, a7.Result, a8.Result);
        }

        /// <summary>
        /// Multiple aggregators sharing a subscription.
        /// </summary>
        /// <remarks>Blocking. Intended to be used with synchronous aggregators only.</remarks>
        public static TResult MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(
            this IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            AggregatorDelegate<TSource, TAggregate3> aggregator3,
            AggregatorDelegate<TSource, TAggregate4> aggregator4,
            AggregatorDelegate<TSource, TAggregate5> aggregator5,
            AggregatorDelegate<TSource, TAggregate6> aggregator6,
            AggregatorDelegate<TSource, TAggregate7> aggregator7,
            AggregatorDelegate<TSource, TAggregate8> aggregator8,
            Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult> resultSelector)
            => source.Async()
                .MultiAggregate(aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, aggregator8, resultSelector, default)
                .GetAwaiter().GetResult();

        /// <summary>
        /// Multiple consumers sharing a subscription.
        /// </summary>
        public static async Task MultiConsume<TSource>(
            this IAsyncEnumerable<TSource> source,
            ConsumerDelegate<TSource> consumer1,
            ConsumerDelegate<TSource> consumer2,
            ConsumerDelegate<TSource> consumer3,
            ConsumerDelegate<TSource> consumer4,
            ConsumerDelegate<TSource> consumer5,
            ConsumerDelegate<TSource> consumer6,
            ConsumerDelegate<TSource> consumer7,
            ConsumerDelegate<TSource> consumer8,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (consumer1 == null) throw new ArgumentNullException(nameof(consumer1));
            if (consumer2 == null) throw new ArgumentNullException(nameof(consumer2));
            if (consumer3 == null) throw new ArgumentNullException(nameof(consumer3));
            if (consumer4 == null) throw new ArgumentNullException(nameof(consumer4));
            if (consumer5 == null) throw new ArgumentNullException(nameof(consumer5));
            if (consumer6 == null) throw new ArgumentNullException(nameof(consumer6));
            if (consumer7 == null) throw new ArgumentNullException(nameof(consumer7));
            if (consumer8 == null) throw new ArgumentNullException(nameof(consumer8));
            token.ThrowIfCancellationRequested();

            var ctx = new MultiContext<TSource>(8, token);
            ctx.Consume(consumer1);
            ctx.Consume(consumer2);
            ctx.Consume(consumer3);
            ctx.Consume(consumer4);
            ctx.Consume(consumer5);
            ctx.Consume(consumer6);
            ctx.Consume(consumer7);
            ctx.Consume(consumer8);
            await ctx.SubscribeTo(source).ConfigureAwait(false);
        }

    }
}
