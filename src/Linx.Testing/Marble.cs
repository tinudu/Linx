namespace Linx.Testing
{
    using Notifications;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Timing;

    public static partial class Marble
    {
        /// <summary>
        /// Assert that <paramref name="actual"/> represents the same sequence as <paramref name="expected"/>.
        /// </summary>
        public static async Task AssertEqual<T>(this IMarbleDiagram<T> expected, IAsyncEnumerable<T> actual, DateTimeOffset now = default, IEqualityComparer<T> elementComparer = null)
            => await VirtualTime.Run(() => AssetEqualCore(expected.Absolute(now), actual, default, elementComparer), now).ConfigureAwait(false);

        /// <summary>
        /// Assert that <paramref name="actual"/> represents the same (canceled) sequence as <paramref name="expected"/>.
        /// </summary>
        public static async Task AssertEqualCancel<T>(this IMarbleDiagram<T> expected, IAsyncEnumerable<T> actual, TimeSpan cancelAfter, DateTimeOffset now = default, IEqualityComparer<T> elementComparer = null)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            var cancelAt = now + cancelAfter;
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var cts = new CancellationTokenSource();
#pragma warning restore IDE0067 // Dispose objects before losing scope
            var exp = expected
                .Absolute(now)
                .TakeWhile(ts => ts.Timestamp < cancelAt)
                .Append(new Timestamped<Notification<T>>(cancelAt, Notification.Error<T>(new OperationCanceledException(cts.Token))));
            await VirtualTime.Run(async () =>
            {
                var time = Time.Current;
                time.Schedule(cts.Cancel, cancelAt, default);
                await AssetEqualCore(exp, actual, cts.Token, elementComparer).ConfigureAwait(false);
            }, now).ConfigureAwait(false);
        }

        /// <summary>
        /// Assert that <paramref name="consumer"/> gets canceld after the specified interval.
        /// </summary>
        public static async Task AssertCancel(Func<CancellationToken, Task> consumer, TimeSpan cancelAfter, DateTimeOffset now = default)
        {
            if (consumer == null) throw new ArgumentNullException(nameof(consumer));

            var cancelAt = now + cancelAfter;
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var cts = new CancellationTokenSource();
#pragma warning restore IDE0067 // Dispose objects before losing scope
            var tsEx = await VirtualTime.Run(async () =>
            {
                var time = Time.Current;
                time.Schedule(cts.Cancel, cancelAt, default);
                try
                {
                    await consumer(cts.Token).ConfigureAwait(false);
                    return null;
                }
                catch (Exception ex) { return ex; }
            }, now).ConfigureAwait(false);

            if (tsEx.Value == null)
                throw new Exception($"Consumer returned sucessfully @{tsEx.Timestamp}.");
            if (!(tsEx.Value is OperationCanceledException oce) || oce.CancellationToken != cts.Token)
                throw new Exception($"Consumer threw an exception other than an OCE on the specified token.@{tsEx.Timestamp}", tsEx.Value);
            if (tsEx.Timestamp != cancelAt)
                throw new Exception($"Consumer canceld @{tsEx.Value}. Expected {cancelAt}.");
        }

        private static async Task AssetEqualCore<T>(IEnumerable<Timestamped<Notification<T>>> expected, IAsyncEnumerable<T> actual, CancellationToken token, IEqualityComparer<T> elementComparer)
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
