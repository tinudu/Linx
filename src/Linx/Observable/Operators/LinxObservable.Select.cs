namespace Linx.Observable
{
    using System;

    partial class LinxObservable
    {
        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static ILinxObservable<TResult> Select<TSource, TResult>(this ILinxObservable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return Create<TResult>(observer => source.Subscribe(
                value => observer.OnNext(selector(value)),
                observer.OnError,
                observer.OnCompleted,
                observer.Token));
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static ILinxObservable<TResult> Select<TSource, TResult>(this ILinxObservable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var i = 0;
            return Create<TResult>(observer => source.Subscribe(
                value => observer.OnNext(selector(value, i++)),
                observer.OnError,
                observer.OnCompleted,
                observer.Token));
        }
    }
}
