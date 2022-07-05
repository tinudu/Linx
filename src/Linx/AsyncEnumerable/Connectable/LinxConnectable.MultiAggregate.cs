using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxConnectable
{
    /// <summary>
    /// Build multiple aggregates in one enumeration.
    /// </summary>
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        Func<T1, T2, TResult> resultSelector,
        CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
        if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        return MultiAggregator<TSource, T1, T2, TResult>.Aggregate(source, aggregator1, aggregator2, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            Func<T1, T2, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 2;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, T3, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        AggregatorDelegate<TSource, T3> aggregator3,
        Func<T1, T2, T3, TResult> resultSelector,
        CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
        if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
        if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        return MultiAggregator<TSource, T1, T2, T3, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, T3, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            AggregatorDelegate<TSource, T3> aggregator3,
            Func<T1, T2, T3, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, T3, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            multi.Subscribe(subject, aggregator3, (ma, a) => ma._aggregate3 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!, multi._aggregate3!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 3;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;
        private T3? _aggregate3;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, T3, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, T3, T4, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        AggregatorDelegate<TSource, T3> aggregator3,
        AggregatorDelegate<TSource, T4> aggregator4,
        Func<T1, T2, T3, T4, TResult> resultSelector,
        CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
        if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
        if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
        if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        return MultiAggregator<TSource, T1, T2, T3, T4, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, T3, T4, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            AggregatorDelegate<TSource, T3> aggregator3,
            AggregatorDelegate<TSource, T4> aggregator4,
            Func<T1, T2, T3, T4, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, T3, T4, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            multi.Subscribe(subject, aggregator3, (ma, a) => ma._aggregate3 = a);
            multi.Subscribe(subject, aggregator4, (ma, a) => ma._aggregate4 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!, multi._aggregate3!, multi._aggregate4!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 4;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;
        private T3? _aggregate3;
        private T4? _aggregate4;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, T3, T4, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, T3, T4, T5, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        AggregatorDelegate<TSource, T3> aggregator3,
        AggregatorDelegate<TSource, T4> aggregator4,
        AggregatorDelegate<TSource, T5> aggregator5,
        Func<T1, T2, T3, T4, T5, TResult> resultSelector,
        CancellationToken token)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (aggregator1 == null) throw new ArgumentNullException(nameof(aggregator1));
        if (aggregator2 == null) throw new ArgumentNullException(nameof(aggregator2));
        if (aggregator3 == null) throw new ArgumentNullException(nameof(aggregator3));
        if (aggregator4 == null) throw new ArgumentNullException(nameof(aggregator4));
        if (aggregator5 == null) throw new ArgumentNullException(nameof(aggregator5));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        return MultiAggregator<TSource, T1, T2, T3, T4, T5, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, T3, T4, T5, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            AggregatorDelegate<TSource, T3> aggregator3,
            AggregatorDelegate<TSource, T4> aggregator4,
            AggregatorDelegate<TSource, T5> aggregator5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, T3, T4, T5, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            multi.Subscribe(subject, aggregator3, (ma, a) => ma._aggregate3 = a);
            multi.Subscribe(subject, aggregator4, (ma, a) => ma._aggregate4 = a);
            multi.Subscribe(subject, aggregator5, (ma, a) => ma._aggregate5 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!, multi._aggregate3!, multi._aggregate4!, multi._aggregate5!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 5;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;
        private T3? _aggregate3;
        private T4? _aggregate4;
        private T5? _aggregate5;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, T3, T4, T5, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, T3, T4, T5, T6, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        AggregatorDelegate<TSource, T3> aggregator3,
        AggregatorDelegate<TSource, T4> aggregator4,
        AggregatorDelegate<TSource, T5> aggregator5,
        AggregatorDelegate<TSource, T6> aggregator6,
        Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector,
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

        return MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            AggregatorDelegate<TSource, T3> aggregator3,
            AggregatorDelegate<TSource, T4> aggregator4,
            AggregatorDelegate<TSource, T5> aggregator5,
            AggregatorDelegate<TSource, T6> aggregator6,
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            multi.Subscribe(subject, aggregator3, (ma, a) => ma._aggregate3 = a);
            multi.Subscribe(subject, aggregator4, (ma, a) => ma._aggregate4 = a);
            multi.Subscribe(subject, aggregator5, (ma, a) => ma._aggregate5 = a);
            multi.Subscribe(subject, aggregator6, (ma, a) => ma._aggregate6 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!, multi._aggregate3!, multi._aggregate4!, multi._aggregate5!, multi._aggregate6!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 6;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;
        private T3? _aggregate3;
        private T4? _aggregate4;
        private T5? _aggregate5;
        private T6? _aggregate6;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, T3, T4, T5, T6, T7, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        AggregatorDelegate<TSource, T3> aggregator3,
        AggregatorDelegate<TSource, T4> aggregator4,
        AggregatorDelegate<TSource, T5> aggregator5,
        AggregatorDelegate<TSource, T6> aggregator6,
        AggregatorDelegate<TSource, T7> aggregator7,
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector,
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

        return MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            AggregatorDelegate<TSource, T3> aggregator3,
            AggregatorDelegate<TSource, T4> aggregator4,
            AggregatorDelegate<TSource, T5> aggregator5,
            AggregatorDelegate<TSource, T6> aggregator6,
            AggregatorDelegate<TSource, T7> aggregator7,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            multi.Subscribe(subject, aggregator3, (ma, a) => ma._aggregate3 = a);
            multi.Subscribe(subject, aggregator4, (ma, a) => ma._aggregate4 = a);
            multi.Subscribe(subject, aggregator5, (ma, a) => ma._aggregate5 = a);
            multi.Subscribe(subject, aggregator6, (ma, a) => ma._aggregate6 = a);
            multi.Subscribe(subject, aggregator7, (ma, a) => ma._aggregate7 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!, multi._aggregate3!, multi._aggregate4!, multi._aggregate5!, multi._aggregate6!, multi._aggregate7!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 7;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;
        private T3? _aggregate3;
        private T4? _aggregate4;
        private T5? _aggregate5;
        private T6? _aggregate6;
        private T7? _aggregate7;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
    public static ValueTask<TResult> MultiAggregate<TSource, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, T1> aggregator1,
        AggregatorDelegate<TSource, T2> aggregator2,
        AggregatorDelegate<TSource, T3> aggregator3,
        AggregatorDelegate<TSource, T4> aggregator4,
        AggregatorDelegate<TSource, T5> aggregator5,
        AggregatorDelegate<TSource, T6> aggregator6,
        AggregatorDelegate<TSource, T7> aggregator7,
        AggregatorDelegate<TSource, T8> aggregator8,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector,
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

        return MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, T8, TResult>.Aggregate(source, aggregator1, aggregator2, aggregator3, aggregator4, aggregator5, aggregator6, aggregator7, aggregator8, resultSelector, token);
    }

    private sealed class MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, T8, TResult>
    {
        public static async ValueTask<TResult> Aggregate(
            IConnectable<TSource> source,
            AggregatorDelegate<TSource, T1> aggregator1,
            AggregatorDelegate<TSource, T2> aggregator2,
            AggregatorDelegate<TSource, T3> aggregator3,
            AggregatorDelegate<TSource, T4> aggregator4,
            AggregatorDelegate<TSource, T5> aggregator5,
            AggregatorDelegate<TSource, T6> aggregator6,
            AggregatorDelegate<TSource, T7> aggregator7,
            AggregatorDelegate<TSource, T8> aggregator8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var multi = new MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(token);
            var subject = source.CreateSubject();
            multi.Subscribe(subject, aggregator1, (ma, a) => ma._aggregate1 = a);
            multi.Subscribe(subject, aggregator2, (ma, a) => ma._aggregate2 = a);
            multi.Subscribe(subject, aggregator3, (ma, a) => ma._aggregate3 = a);
            multi.Subscribe(subject, aggregator4, (ma, a) => ma._aggregate4 = a);
            multi.Subscribe(subject, aggregator5, (ma, a) => ma._aggregate5 = a);
            multi.Subscribe(subject, aggregator6, (ma, a) => ma._aggregate6 = a);
            multi.Subscribe(subject, aggregator7, (ma, a) => ma._aggregate7 = a);
            multi.Subscribe(subject, aggregator8, (ma, a) => ma._aggregate8 = a);
            subject.Connect();
            await multi._atmbWhenAll.Task.ConfigureAwait(false);
            return resultSelector(multi._aggregate1!, multi._aggregate2!, multi._aggregate3!, multi._aggregate4!, multi._aggregate5!, multi._aggregate6!, multi._aggregate7!, multi._aggregate8!);
        }

        private readonly CancellationTokenSource _cts = new();
        private readonly CancellationTokenRegistration _ctr;
        private int _active = 8;
        private Exception? _error;
        private AsyncTaskMethodBuilder _atmbWhenAll = new();
        private T1? _aggregate1;
        private T2? _aggregate2;
        private T3? _aggregate3;
        private T4? _aggregate4;
        private T5? _aggregate5;
        private T6? _aggregate6;
        private T7? _aggregate7;
        private T8? _aggregate8;

        private MultiAggregator(CancellationToken token)
        {
            if (token.CanBeCanceled)
                _ctr = token.Register(() => OnError(new OperationCanceledException(token)));
        }

        private async void Subscribe<TAggregate>(ISubject<TSource> source, AggregatorDelegate<TSource, TAggregate> aggregator, Action<MultiAggregator<TSource, T1, T2, T3, T4, T5, T6, T7, T8, TResult>, TAggregate> setResult)
        {
            try
            {
                _cts.Token.ThrowIfCancellationRequested();
                setResult(this, await aggregator(source.AsyncEnumerable, _cts.Token).ConfigureAwait(false));
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
