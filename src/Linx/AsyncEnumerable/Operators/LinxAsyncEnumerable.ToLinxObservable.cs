namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Observable;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Convert a <see cref="IEnumerable{T}"/> to a <see cref="ILinxObservable{T}"/>.
        /// </summary>
        public static ILinxObservable<T> ToLinxObservable<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return LinxObservable.Create<T>(async observer =>
            {
                try
                {
                    var ae = source.WithCancellation(observer.Token).ConfigureAwait(false).GetAsyncEnumerator();
                    try
                    {
                        while(await ae.MoveNextAsync())
                            if (!observer.OnNext(ae.Current))
                                break;
                    }
                    finally { await ae.DisposeAsync();}

                    observer.OnCompleted();
                }
                catch (Exception ex) { observer.OnError(ex); }
            }, source + ".ToLinxObservable");
        }
    }
}
