namespace Linx.Observable
{
    using System;
    using Timing;

    partial class LinxObservable
    {
        /// <summary>
        /// Records the timestamp for each value.
        /// </summary>
        public static ILinxObservable<Timestamped<T>> Timestamp<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<Timestamped<T>>(observer =>
            {
                var time = Time.Current;
                source.SafeSubscribe(
                    value => observer.OnNext(new Timestamped<T>(time.Now, value)),
                    observer.OnError,
                    observer.OnCompleted,
                    observer.Token);
            });
        }
    }
}
