namespace Linx.Observable
{
    using System;

    partial class LinxObservable
    {
        /// <summary>
        /// Filters a sequence of values based on a predicate.
        /// </summary>
        public static ILinxObservable<T> Where<T>(this ILinxObservable<T> source, Func<T, bool> predicte)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (predicte == null) throw new ArgumentNullException(nameof(predicte));

            return Create<T>(observer => source.Subscribe(
                value => predicte(value) && observer.OnNext(value),
                observer.OnError,
                observer.OnCompleted,
                observer.Token));
        }
    }
}
