namespace Linx.Observable
{
    using System;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static ILinxObservable<TResult> Select<TSource, TResult>(this ILinxObservable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(observer =>
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                source.SafeSubscribe(
                    value =>
                    {
                        try
                        {
                            return observer.OnNext(selector(value));
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
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static ILinxObservable<TResult> Select<TSource, TResult>(this ILinxObservable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(observer =>
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                var i = 0;
                source.SafeSubscribe(
                    value =>
                    {
                        try
                        {
                            return observer.OnNext(selector(value, Interlocked.Increment(ref i)));
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
