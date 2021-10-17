using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Linx.Notifications;
using Linx.Timing;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Replays the recorded sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Replay<T>(this IEnumerable<TimeInterval<Notification<T>>> recorded, ITime time)
        {
            if (recorded is null) throw new ArgumentNullException(nameof(recorded));
            if (time is null) time = Time.RealTime;

            return Iterator().Dematerialize();

            async IAsyncEnumerable<Notification<T>> Iterator([EnumeratorCancellation] CancellationToken token = default)
            {
                var t = time.Now;
                using var timer = time.GetTimer(token);
                foreach (var tn in recorded)
                {
                    t += tn.Interval;
                    await timer.Delay(t).ConfigureAwait(false);
                    yield return tn.Value;
                }
            }
        }
    }
}
