namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Indicates the sequence by <paramref name="delay"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Delay<T>(this IAsyncEnumerable<T> source, TimeSpan delay)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (delay <= TimeSpan.Zero) return source;

            return Produce<T>(async (yield, token) =>
            {
                var time = Time.Current;
                var ae = source
                    .Materialize()
                    .Timestamp()
                    .Buffer()
                    .WithCancellation(token)
                    .ConfigureAwait(false)
                    .GetAsyncEnumerator();
                try
                {
                    using (var timer = Time.Current.GetTimer(token))
                        while (await ae.MoveNextAsync())
                        {
                            var current = ae.Current;
                            await timer.Delay(current.Timestamp + delay).ConfigureAwait(false);
                            switch (current.Value.Kind)
                            {
                                case NotificationKind.Next:
                                    await yield(current.Value.Value).ConfigureAwait(false);
                                    break;
                                case NotificationKind.Error:
                                    throw current.Value.Error;
                                case NotificationKind.Completed:
                                    return;
                                default:
                                    throw new Exception(current.Value.Kind + "???");
                            }
                        }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
