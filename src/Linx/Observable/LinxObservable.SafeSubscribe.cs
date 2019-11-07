﻿namespace Linx.Observable
{
    using System;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Safely subscribe the <paramref name="observer"/> to the <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="ILinxObserver{T}.Token"/> requests cancellation, or if <see cref="ILinxObservable{T}.Subscribe"/> throws an exception, the observer is notified.
        /// </remarks>
        public static void SafeSubscribe<T>(this ILinxObservable<T> source, ILinxObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            try
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                observer.Token.ThrowIfCancellationRequested();
                source.Subscribe(observer);
            }
            catch (Exception ex) { observer.OnError(ex); }
        }

        /// <summary>
        /// Safely subscribe an observer cretaed from the specified delegates and token to the <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="ILinxObserver{T}.Token"/> requests cancellation, or if <see cref="ILinxObservable{T}.Subscribe"/> throws an exception, the observer is notified.
        /// </remarks>
        public static void SafeSubscribe<T>(
            this ILinxObservable<T> source,
            Func<T, bool> onNext,
            Action<Exception> onError,
            Action onCompleted,
            CancellationToken token)
        {
            if (onNext == null) throw new ArgumentNullException(nameof(onNext));
            if (onError == null) throw new ArgumentNullException(nameof(onError));
            if (onCompleted == null) throw new ArgumentNullException(nameof(onCompleted));

            try
            {
                if (source == null) throw new ArgumentNullException(nameof(source));
                token.ThrowIfCancellationRequested();
                source.Subscribe(new AnonymousLinxObserver<T>(onNext, onError, onCompleted, token));
            }
            catch (Exception ex) { onError(ex); }

        }
    }
}