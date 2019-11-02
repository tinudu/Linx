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

        /// <inheritdoc />
        public override string ToString() => nameof(MarbleDiagram<T>);
    }
}
