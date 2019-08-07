namespace Linx.Observable
{
    using System;
    using Timing;

    partial class LinxObservable
    {
        /// <summary>
        /// Records the time interval between consecutive values.
        /// </summary>
        public static ILinxObservable<TimeInterval<T>> TimeInterval<T>(this ILinxObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<TimeInterval<T>>(observer =>
            {
                try
                {
                    var time = Time.Current;
                    var prev = time.Now;
                    source.Subscribe(
                        value =>
                        {
                            var now = time.Now;
                            var interval = now - prev;
                            prev = now;
                            return observer.OnNext(new TimeInterval<T>(interval, value));
                        },
                        observer.OnError,
                        observer.OnCompleted,
                        observer.Token);
                }
                catch (Exception ex) { observer.OnError(ex); }
            });
        }
    }
}
