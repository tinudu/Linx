namespace Linx.Testing
{
    using AsyncEnumerable;
    using Notifications;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Timing;

    internal sealed class MarbleDiagram<T> : AsyncEnumerableBase<T>, IMarbleDiagram<T>
    {
        public IEnumerable<TimeInterval<Notification<T>>> Marbles { get; }

        public MarbleDiagram(IEnumerable<TimeInterval<Notification<T>>> marbles) => Marbles = marbles;

        /// <inheritdoc />
        public override async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
        {
            using var timer = Time.Current.GetTimer(token);
            var t = timer.Time.Now;
            foreach (var ti in Marbles)
            {
                t += ti.Interval;
                var n = ti.Value;
                await timer.Delay(t).ConfigureAwait(false);
                switch (n.Kind)
                {
                    case NotificationKind.Next:
                        yield return n.Value;
                        break;
                    case NotificationKind.Completed:
                        yield break;
                    case NotificationKind.Error:
                        throw n.Error;
                    default:
                        throw new Exception(ti.Value.Kind + "???");
                }
            }

            await token.WhenCanceled().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override string ToString() => nameof(MarbleDiagram<T>);
    }
}
