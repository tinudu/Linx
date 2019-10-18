namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TResult>(this
            IAsyncEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            Func<TAggregate1, TAggregate2, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TResult>.Aggregate(source, aggregator1, aggregator2, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TResult>(this
            IEnumerable<TSource> source,
            AggregatorDelegate<TSource, TAggregate1> aggregator1,
            AggregatorDelegate<TSource, TAggregate2> aggregator2,
            Func<TAggregate1, TAggregate2, TResult> resultSelector,
            CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
            if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
                AggregatorDelegate<TSource, TAggregate1> aggregator1,
                AggregatorDelegate<TSource, TAggregate2> aggregator2,
                Func<TAggregate1, TAggregate2, TResult> resultSelector,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 2;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(this
            IAsyncEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(this
            IEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, aggregator3, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
                AggregatorDelegate<TSource, TAggregate1> aggregator1,
                AggregatorDelegate<TSource, TAggregate2> aggregator2,
                AggregatorDelegate<TSource, TAggregate3> aggregator3,
                Func<TAggregate1, TAggregate2, TAggregate3, TResult> resultSelector,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                multi.Subscribe(connectable, aggregator3, (ma, a) => ma._aggregate3 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2, multi._aggregate3);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 3;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;
            private TAggregate3 _aggregate3;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(this
            IAsyncEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(this
            IEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, aggregator3, aggregator4, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
                AggregatorDelegate<TSource, TAggregate1> aggregator1,
                AggregatorDelegate<TSource, TAggregate2> aggregator2,
                AggregatorDelegate<TSource, TAggregate3> aggregator3,
                AggregatorDelegate<TSource, TAggregate4> aggregator4,
                Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult> resultSelector,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                multi.Subscribe(connectable, aggregator3, (ma, a) => ma._aggregate3 = a);
                multi.Subscribe(connectable, aggregator4, (ma, a) => ma._aggregate4 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2, multi._aggregate3, multi._aggregate4);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 4;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;
            private TAggregate3 _aggregate3;
            private TAggregate4 _aggregate4;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(this
            IAsyncEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(this
            IEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
                AggregatorDelegate<TSource, TAggregate1> aggregator1,
                AggregatorDelegate<TSource, TAggregate2> aggregator2,
                AggregatorDelegate<TSource, TAggregate3> aggregator3,
                AggregatorDelegate<TSource, TAggregate4> aggregator4,
                AggregatorDelegate<TSource, TAggregate5> aggregator5,
                Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult> resultSelector,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                multi.Subscribe(connectable, aggregator3, (ma, a) => ma._aggregate3 = a);
                multi.Subscribe(connectable, aggregator4, (ma, a) => ma._aggregate4 = a);
                multi.Subscribe(connectable, aggregator5, (ma, a) => ma._aggregate5 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2, multi._aggregate3, multi._aggregate4, multi._aggregate5);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 5;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;
            private TAggregate3 _aggregate3;
            private TAggregate4 _aggregate4;
            private TAggregate5 _aggregate5;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(this
            IAsyncEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(this
            IEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
                AggregatorDelegate<TSource, TAggregate1> aggregator1,
                AggregatorDelegate<TSource, TAggregate2> aggregator2,
                AggregatorDelegate<TSource, TAggregate3> aggregator3,
                AggregatorDelegate<TSource, TAggregate4> aggregator4,
                AggregatorDelegate<TSource, TAggregate5> aggregator5,
                AggregatorDelegate<TSource, TAggregate6> aggregator6,
                Func<TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult> resultSelector,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                multi.Subscribe(connectable, aggregator3, (ma, a) => ma._aggregate3 = a);
                multi.Subscribe(connectable, aggregator4, (ma, a) => ma._aggregate4 = a);
                multi.Subscribe(connectable, aggregator5, (ma, a) => ma._aggregate5 = a);
                multi.Subscribe(connectable, aggregator6, (ma, a) => ma._aggregate6 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2, multi._aggregate3, multi._aggregate4, multi._aggregate5, multi._aggregate6);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 6;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;
            private TAggregate3 _aggregate3;
            private TAggregate4 _aggregate4;
            private TAggregate5 _aggregate5;
            private TAggregate6 _aggregate6;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(this
            IAsyncEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(this
            IEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
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
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                multi.Subscribe(connectable, aggregator3, (ma, a) => ma._aggregate3 = a);
                multi.Subscribe(connectable, aggregator4, (ma, a) => ma._aggregate4 = a);
                multi.Subscribe(connectable, aggregator5, (ma, a) => ma._aggregate5 = a);
                multi.Subscribe(connectable, aggregator6, (ma, a) => ma._aggregate6 = a);
                multi.Subscribe(connectable, aggregator7, (ma, a) => ma._aggregate7 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2, multi._aggregate3, multi._aggregate4, multi._aggregate5, multi._aggregate6, multi._aggregate7);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 7;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;
            private TAggregate3 _aggregate3;
            private TAggregate4 _aggregate4;
            private TAggregate5 _aggregate5;
            private TAggregate6 _aggregate6;
            private TAggregate7 _aggregate7;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(this
            IAsyncEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, aggregator8, resultSelector, token);
        }

        /// <summary>
        /// Build multiple aggregates in one enumeration.
        /// </summary>
        public static Task<TResult> MultiAggregate<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(this
            IEnumerable<TSource> source,
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

            return MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>.Aggregate(source.Async(), aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, aggregator8, resultSelector, token);
        }

        private sealed class MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>
        {
            public static async Task<TResult> Aggregate(
                IAsyncEnumerable<TSource> source,
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
                token.ThrowIfCancellationRequested();
                var multi = new MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>(token);
                var connectable = source.Connectable(out var connect);
                // ReSharper disable PossibleMultipleEnumeration
                multi.Subscribe(connectable, aggregator1, (ma, a) => ma._aggregate1 = a);
                multi.Subscribe(connectable, aggregator2, (ma, a) => ma._aggregate2 = a);
                multi.Subscribe(connectable, aggregator3, (ma, a) => ma._aggregate3 = a);
                multi.Subscribe(connectable, aggregator4, (ma, a) => ma._aggregate4 = a);
                multi.Subscribe(connectable, aggregator5, (ma, a) => ma._aggregate5 = a);
                multi.Subscribe(connectable, aggregator6, (ma, a) => ma._aggregate6 = a);
                multi.Subscribe(connectable, aggregator7, (ma, a) => ma._aggregate7 = a);
                multi.Subscribe(connectable, aggregator8, (ma, a) => ma._aggregate8 = a);
                // ReSharper restore PossibleMultipleEnumeration
                connect();
                await multi._atmbWhenAll.Task.ConfigureAwait(false);
                return resultSelector(multi._aggregate1, multi._aggregate2, multi._aggregate3, multi._aggregate4, multi._aggregate5, multi._aggregate6, multi._aggregate7, multi._aggregate8);
            }

            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private CancellationTokenRegistration _ctr;
            private int _active = 8;
            private Exception _error;
            private AsyncTaskMethodBuilder _atmbWhenAll = new AsyncTaskMethodBuilder();
            private TAggregate1 _aggregate1;
            private TAggregate2 _aggregate2;
            private TAggregate3 _aggregate3;
            private TAggregate4 _aggregate4;
            private TAggregate5 _aggregate5;
            private TAggregate6 _aggregate6;
            private TAggregate7 _aggregate7;
            private TAggregate8 _aggregate8;

            private MultiAggregator(CancellationToken token)
            {
                if (token.CanBeCanceled) _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
            }

            private async void Subscribe<TAggregate>(IAsyncEnumerable<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, TAggregate1, TAggregate2, TAggregate3, TAggregate4, TAggregate5, TAggregate6, TAggregate7, TAggregate8, TResult>, TAggregate> setResult)
            {
                try
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    setResult(this, await aggregator(source, _cts.Token).ConfigureAwait(false));
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