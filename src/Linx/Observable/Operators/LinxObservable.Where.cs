namespace Linx.Observable
{
    using System;

    partial class LinxObservable
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static ILinxObservable<T> Where<T>(this ILinxObservable<T> source, Func<T, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create<T>(observer =>
            {
                try
                {
                    source.Subscribe(
                        value => !predicate(value) && observer.OnNext(value),
                        observer.OnError,
                        observer.OnCompleted,
                        observer.Token);
                }
                catch (Exception ex) { observer.OnError(ex); }
            });
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static ILinxObservable<T> Where<T>(this ILinxObservable<T> source, Func<T, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            return Create<T>(observer =>
            {
                try
                {
                    var i = 0;
                    source.Subscribe(
                        value => !predicate(value, i++) && observer.OnNext(value),
                        observer.OnError,
                        observer.OnCompleted,
                        observer.Token);
                }
                catch (Exception ex) { observer.OnError(ex); }
            });
        }
    }
}
