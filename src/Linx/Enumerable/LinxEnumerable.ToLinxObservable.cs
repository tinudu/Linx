namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using Observable;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Convert a <see cref="IEnumerable{T}"/> to a <see cref="ILinxObservable{T}"/>.
        /// </summary>
        public static ILinxObservable<T> ToLinxObservable<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LinxObservable.Create<T>(observer =>
            {
                try
                {
                    foreach (var element in source)
                        if (!observer.OnNext(element))
                            break;
                    observer.OnCompleted();
                }
                catch (Exception ex) { observer.OnError(ex); }
            });
        }
    }
}
