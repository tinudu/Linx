namespace Linx.Observable
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Convert a <see cref="IObservable{T}"/> to a <see cref="ILinxObservable{T}"/>.
        /// </summary>
        /// <remarks>
        /// Because <see cref="IObservable{T}"/>s do not support asynchronous disposal,
        /// all that can be done in case of a cancellation request from the <see cref="ILinxObserver{T}"/>
        /// is to dispose of the subscription (as soon as available) and acknowlede disposable immediately.
        /// </remarks>
        public static ILinxObservable<T> ToLinxObservable<T>(this IObservable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Create<T>(Subscribe);

            void Subscribe(ILinxObserver<T> observer)
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                var anobs = new AnonymousObserver<T>(observer);
                try
                {
                    var subscription = source.Subscribe(anobs);

                    var awaiter = anobs.Completed.ConfigureAwait(false).GetAwaiter();
                    if (awaiter.IsCompleted)
                        subscription.Dispose();
                    else awaiter.OnCompleted(() =>
                        subscription.Dispose());
                }
                catch (Exception ex) { anobs.OnError(ex); }
            }
        }

        private sealed class AnonymousObserver<T> : IObserver<T>
        {
            private ILinxObserver<T> _observer;
            private AsyncTaskMethodBuilder _atmbCompleted = new AsyncTaskMethodBuilder();
            private CancellationTokenRegistration _ctr;

            public AnonymousObserver(ILinxObserver<T> observer)
            {
                _observer = observer;
                if (!observer.Token.CanBeCanceled) return;
                var token = observer.Token;
                _ctr = token.Register(() => Complete(new OperationCanceledException(token)));
            }

            public Task Completed => _atmbCompleted.Task;

            void IObserver<T>.OnNext(T value)
            {
                var lobs = _observer;
                if (lobs == null) return;
                Exception error;
                try
                {
                    if (lobs.OnNext(value)) return;
                    error = null;
                }
                catch (Exception ex) { error = ex; }
                Complete(error);
            }

            public void OnError(Exception error) => Complete(error);

            void IObserver<T>.OnCompleted() => Complete(null);

            private void Complete(Exception errorOpt)
            {
                var lobs = Interlocked.Exchange(ref _observer, null);
                if (lobs == null) return;

                _ctr.Dispose();
                _atmbCompleted.SetResult();
                if (errorOpt == null)
                    lobs.OnCompleted();
                else
                    lobs.OnError(errorOpt);
            }
        }
    }
}
