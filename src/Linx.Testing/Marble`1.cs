namespace Linx.Testing
{
    using Notifications;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Timing;

    /// <summary>
    /// A marble diagram.
    /// </summary>
    public sealed class Marble<T> : IAsyncEnumerable<T>
    {
        /// <summary>
        /// Gets the marbles.
        /// </summary>
        public IEnumerable<TimeInterval<Notification<T>>> Marbles { get; }

        internal Marble(IEnumerable<TimeInterval<Notification<T>>> marbles) => Marbles = marbles;

        /// <inheritdoc />
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token = new CancellationToken())
        {
            var time = Time.Current;
            var t = time.Now;
            using var timer = time.GetTimer(token);
            foreach (var ti in Marbles)
            {
                t += ti.Interval;
                await timer.Delay(t).ConfigureAwait(false);
                switch (ti.Value.Kind)
                {
                    case NotificationKind.Next:
                        yield return ti.Value.Value;
                        break;
                    case NotificationKind.Completed:
                        yield break;
                    case NotificationKind.Error:
                        throw ti.Value.Error;
                    default:
                        throw new Exception(ti.Value.Kind + "???");
                }
            }

            await token.WhenCanceled().ConfigureAwait(false);
        }

        /// <summary>
        /// Assert that <paramref name="testee"/> represents the same sequence as this marble diagram.
        /// </summary>
        public async Task AssertEqual(IAsyncEnumerable<T> testee, CancellationToken token)
        {
            if (testee == null) throw new ArgumentNullException(nameof(testee));

            var time = Time.Current;
            var position = 1;
            // ReSharper disable once GenericEnumeratorNotDisposed
            using var e = Timestamped(time.Now).GetEnumerator();
            await using var ae = testee.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            while (true)
            {
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
        }

        private IEnumerable<Timestamped<Notification<T>>> Timestamped(DateTimeOffset time)
        {
            foreach (var ti in Marbles)
            {
                time += ti.Interval;
                yield return new Timestamped<Notification<T>>(time, ti.Value);
            }
        }
    }
}
