namespace Linx.Observable
{
    using System;
    using System.Threading;

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
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                source.SafeSubscribe(
                    value =>
                    {
                        try
                        {
                            return !predicate(value) || observer.OnNext(value);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return false;
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted,
                    observer.Token);
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
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                var i = 0;
                source.SafeSubscribe(
                    value =>
                    {
                        try
                        {
                            return !predicate(value, Interlocked.Increment(ref i)) || observer.OnNext(value);
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return false;
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted,
                    observer.Token);
            });
        }
    }
}
