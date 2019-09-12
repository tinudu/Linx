namespace Linx.Observable
{
    using System;

    partial class LinxObservable
    {
        /// <summary>
        /// Try to subscribe the specified observer to the source and notify the observer in case of an error.
        /// </summary>
        public static void SubscribeCatch<T>(this ILinxObservable<T> source, ILinxObserver<T> observer)
        {
            try
            {
                observer.Token.ThrowIfCancellationRequested();
                source.Subscribe(observer); 
            }
            catch (Exception ex) { observer.OnError(ex); }
        }
    }
}
