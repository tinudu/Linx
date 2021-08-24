using System;
using System.Collections.Generic;

namespace Linx.LinxObservable
{
    partial class LinxObservable
    {
        /// <summary>
        /// Wraps a synchronous <see cref="IEnumerable{T}"/> into an <see cref="ILinxObservable{T}"/>.
        /// </summary>
        public static ILinxObservable<T> ToLinxObservable<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<T>(observer =>
            {
                try
                {
                    foreach (var item in source)
                    {
                        observer.OnNext(item);
                        observer.Token.ThrowIfCancellationRequested();
                    }
                    observer.OnCompleted();
                }
                catch (Exception error) { observer.OnError(error); }
            });
        }
    }
}
