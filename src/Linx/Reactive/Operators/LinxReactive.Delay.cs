namespace Linx.Reactive
{
    using System;
    using Timing;

    partial class LinxReactive
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
                    .GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                    {
                        var current = ae.Current;
                        await time.Wait(current.Timestamp + delay, token).ConfigureAwait(false);
                        switch (current.Value.Kind)
                        {
                            case NotificationKind.OnNext:
                                await yield(current.Value.Value);
                                break;
                            case NotificationKind.OnError:
                                throw current.Value.Error;
                            case NotificationKind.OnCompleted:
                                return;
                            default:
                                throw new Exception(current.Value.Kind + "???");
                        }
                    }
                }
                finally { await ae.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
