using System;
using System.Collections.Generic;
using System.Threading;
using Linx.Notifications;
using Linx.Timing;

namespace Linx.Testing
{
    internal sealed class MarbleDiagram<T> : IMarbleDiagram<T>
    {
        public IEnumerable<TimeInterval<Notification<T>>> Marbles { get; }

        public MarbleDiagram(IEnumerable<TimeInterval<Notification<T>>> marbles) => Marbles = marbles;

        /// <inheritdoc />
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token)
        {
            var time = Time.Current;
            using var timer = time.GetTimer(token);
            var t = time.Now;
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

            throw await token.WhenCancellationRequested().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override string ToString() => nameof(MarbleDiagram<T>);
    }
}
