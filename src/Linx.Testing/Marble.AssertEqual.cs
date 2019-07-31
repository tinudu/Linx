namespace Linx.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Notifications;
    using Timing;

    partial class Marble
    {
        /// <summary>
        /// Asserts that <paramref name="testee"/> and <paramref name="expectation"/> are equal both element wise as well as the temporal spacing.
        /// </summary>
        /// <exception cref="Exception">Not equal element wise.</exception>
        public static async Task AssertEqual<T>(this IAsyncEnumerable<T> testee, IEnumerable<TimeInterval<Notification<T>>> expectation, CancellationToken token)
        {
            if (testee == null) throw new ArgumentNullException(nameof(testee));
            if (expectation == null) throw new ArgumentNullException(nameof(expectation));

            var position = 0;
            var time = Time.Current;
            using (var e = expectation.Absolute(Time.Current.Now).GetEnumerator())
            {
                var ae = testee.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (true)
                    {
                        // materializing and timestamping during enumeration in order to not get canceled.
                        Notification<T> notification;
                        try { notification = await ae.MoveNextAsync() ? Notification.Next(ae.Current) : Notification.Completed<T>(); }
                        catch (Exception ex) { notification = Notification.Error<T>(ex); }
                        var current = new Timestamped<Notification<T>>(time.Now, notification);

                        if (!e.MoveNext())
                            throw new Exception($"Position {position} - Received {current}, Expected: EOS");

                        var exp = e.Current;
                        if (!current.Equals(exp))
                            throw new Exception($"Position {position} - Received {current}, Expected: {exp}");

                        position++;

                        if (notification.Kind != NotificationKind.Next)
                            break;
                    }

                    if (e.MoveNext())
                        throw new Exception($"Position {position} - Received EOS, Expected: {e.Current}");
                }
                finally { await ae.DisposeAsync(); }
            }
        }

        private static IEnumerable<Timestamped<T>> Absolute<T>(this IEnumerable<TimeInterval<T>> source, DateTimeOffset time)
        {
            foreach (var ti in source)
            {
                time += ti.Interval;
                yield return new Timestamped<T>(time, ti.Value);
            }
        }
    }
}
