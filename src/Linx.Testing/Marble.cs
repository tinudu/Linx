namespace Linx.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Notifications;
    using Timing;

    public static partial class Marble
    {
        /// <summary>
        /// Assert that <paramref name="actual"/> represents the same sequence as <paramref name="expected"/> when enumerated on virtual time.
        /// </summary>
        public static async Task AssertEqual<T>(this IMarbleDiagram<T> expected, IAsyncEnumerable<T> actual, DateTimeOffset startTime = default, IEqualityComparer<T> elementComparer = null)
        {
            // schedule on thread pool so continuations run synchronously
            await Task.Run(async () =>
            {
                using var vt = new VirtualTime(startTime);
                var eq = AssetEqual(expected.Absolute(startTime), actual, default, elementComparer);
                vt.Start();
                await eq.ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Assert that <paramref name="actual"/> represents the same sequence as <paramref name="expected"/> when enumeration is canceled.
        /// </summary>
        public static async Task AssertCancel<T>(this IMarbleDiagram<T> expected, IAsyncEnumerable<T> actual, TimeSpan cancelAfter, DateTimeOffset startTime = default, IEqualityComparer<T> elementComparer = null)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            // schedule on thread pool so continuations run synchronously
            await Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource();
                var cancelAt = startTime + cancelAfter;
                var exp = expected
                    .Absolute(startTime)
                    .TakeWhile(ts => ts.Timestamp < cancelAt)
                    .Append(new Timestamped<Notification<T>>(cancelAt, Notification.Error<T>(new OperationCanceledException(cts.Token))));
                using var vt = new VirtualTime(startTime);
                _ = vt.Schedule(cts.Cancel, cancelAt, default);
                var eq = AssetEqual(exp, actual, cts.Token, elementComparer);
                vt.Start();
                await eq.ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Run the delegate on virtual time.
        /// </summary>
        public static async Task<T> OnVirtualTime<T>(Func<CancellationToken, Task<T>> aggregator, DateTimeOffset startTime = default)
        {
            if (aggregator == null) throw new ArgumentNullException(nameof(aggregator));

            // schedule on thread pool so continuations run synchronously
            return await Task.Run(async () =>
            {
                using var vt = new VirtualTime(startTime);
                var tResult = aggregator(default);
                vt.Start();
                return await tResult.ConfigureAwait(false);
            }).ConfigureAwait(false);

        }

        /// <summary>
        /// Assert that <paramref name="consumer"/> gets canceld after the specified interval.
        /// </summary>
        public static async Task AssertCancel(Func<CancellationToken, Task> consumer, TimeSpan cancelAfter, DateTimeOffset startTime = default)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));

            // schedule on thread pool so continuations run synchronously
            await Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource();
                var cancelAt = startTime + cancelAfter;
                using var vt = new VirtualTime(startTime);
                _ = vt.Schedule(cts.Cancel, cancelAt, default);

                async Task<(DateTimeOffset, Exception)> AwaitResult()
                {
                    // ReSharper disable AccessToDisposedClosure
                    try
                    {
                        await consumer(cts.Token).ConfigureAwait(false);
                        return (vt.Now, null);
                    }
                    catch (Exception x)
                    {
                        return (vt.Now, x);
                    }
                    // ReSharper restore AccessToDisposedClosure
                }

                var tResult = AwaitResult();
                vt.Start();
                var (ts, ex) = await tResult.ConfigureAwait(false);
                if (ex == null)
                    throw new Exception($"Consumer returned sucessfully @{ts}.");
                if (!(ex is OperationCanceledException oce) || oce.CancellationToken != cts.Token)
                    throw new Exception($"Consumer threw an exception other than an OCE on the specified token @{ts}", ex);
                if (ts != cancelAt)
                    throw new Exception($"Consumer canceld @{vt.Now}. Expected {cancelAt}");
            }).ConfigureAwait(false);
        }

        private static async Task AssetEqual<T>(IEnumerable<Timestamped<Notification<T>>> expected, IAsyncEnumerable<T> actual, CancellationToken token, IEqualityComparer<T> elementComparer)
        {
            Debug.Assert(expected != null);
            Debug.Assert(actual != null);
            if (elementComparer == null) elementComparer = EqualityComparer<T>.Default;

            bool Equals(Timestamped<Notification<T>> x, Timestamped<Notification<T>> y)
            {
                if (x.Timestamp != y.Timestamp)
                    return false;

                var nx = x.Value;
                var ny = y.Value;
                switch (nx.Kind)
                {
                    case NotificationKind.Completed:
                        return ny.Kind == NotificationKind.Completed;
                    case NotificationKind.Next:
                        return ny.Kind == NotificationKind.Next && elementComparer.Equals(nx.Value, ny.Value);
                    case NotificationKind.Error:
                        if (ny.Kind != NotificationKind.Error)
                            return false;
                        var xx = nx.Error;
                        var xy = ny.Error;
                        if (xx.GetType() != xy.GetType() || xx.Message != xy.Message)
                            return false;
                        if (xx is OperationCanceledException ocex && xy is OperationCanceledException ocey)
                            return ocex.CancellationToken == ocey.CancellationToken;
                        return true;
                    default:
                        return false;
                }
            }

            var time = Time.Current;
            var position = 1;
            // ReSharper disable once GenericEnumeratorNotDisposed
            using var e = expected.GetEnumerator();
            await using var ae = actual.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            while (true)
            {
                Notification<T> notification;
                try { notification = await ae.MoveNextAsync() ? Notification.Next(ae.Current) : Notification.Completed<T>(); }
                catch (Exception ex) { notification = Notification.Error<T>(ex); }
                var nActual = new Timestamped<Notification<T>>(time.Now, notification);

                if (!e.MoveNext())
                    throw new Exception($"Position {position}: Received {nActual}, Expected: EOS");

                var nExpected = e.Current;
                if (!Equals(nActual, nExpected))
                    throw new Exception($"Position {position}: Received {nActual}, Expected: {nExpected}");

                position++;

                if (notification.Kind != NotificationKind.Next)
                    break;
            }
        }

        private static IEnumerable<Timestamped<Notification<T>>> Absolute<T>(this IMarbleDiagram<T> md, DateTimeOffset time)
        {
            foreach (var ti in md.Marbles)
            {
                time += ti.Interval;
                yield return new Timestamped<Notification<T>>(time, ti.Value);
            }
        }
    }
}
