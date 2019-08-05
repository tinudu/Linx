namespace Linx.Observable
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxObservable
    {
        /// <summary>
        /// Convert a <see cref="IObservable{T}"/> to a <see cref="ILinxObservable{T}"/>.
        /// </summary>
        /// <remarks>Blocking, subscribed on the task pool, best effort disposal.</remarks>
        public static ILinxObservable<T> ToLinxObservable<T>(this IObservable<T> source)
            => new ObservableLinxObservable<T>(source);

        private sealed class ObservableLinxObservable<T> : ILinxObservable<T>
        {
            private readonly IObservable<T> _source;
            public ObservableLinxObservable(IObservable<T> source) => _source = source ?? throw new ArgumentNullException(nameof(source));

            public void Subscribe(ILinxObserver<T> observer)
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                try
                {
                    observer.Token.ThrowIfCancellationRequested();
                    var subscription = new Subscription(observer);
                    Task.Run(() =>
                    {
                        try { subscription.SetDisposable(_source.Subscribe(subscription)); }
                        catch (Exception ex)
                        {
                            subscription.SetDisposable(EmptyDisposable.Default);
                            subscription.OnError(ex);
                        }
                    });
                }
                catch (Exception ex) { observer.OnError(ex); }
            }

            private sealed class Subscription : IObserver<T>
            {
                private readonly object _gate = new object();
                private readonly ILinxObserver<T> _observer;
                private CancellationTokenRegistration _ctr;
                private IDisposable _disposable;
                private bool _isCompleted;
                private Exception _error;

                public Subscription(ILinxObserver<T> observer)
                {
                    _observer = observer;
                    var token = observer.Token;
                    if (token.CanBeCanceled) _ctr = token.Register(() => Complete(new OperationCanceledException(token)));
                }

                public void OnNext(T value)
                {
                    lock (_gate)
                    {
                        if (_isCompleted) return;
                        try { if (!_observer.OnNext(value)) Complete(null); }
                        catch (Exception ex) { Complete(ex); }
                    }
                }

                public void OnError(Exception error) => Complete(error);
                public void OnCompleted() => Complete(null);

                public void SetDisposable(IDisposable disposable)
                {
                    lock (_gate)
                    {
                        if (_disposable != null) throw new InvalidOperationException();
                        _disposable = disposable ?? EmptyDisposable.Default;
                        if (!_isCompleted) return;
                    }
                    try { _disposable.Dispose(); } catch {/**/}
                    if (_error == null) _observer.OnCompleted();
                    else _observer.OnError(_error);
                }

                private void Complete(Exception error)
                {
                    IDisposable disposable;
                    lock (_gate)
                    {
                        if (Linx.Exchange(ref _isCompleted, true)) return;
                        _error = error;
                        _ctr.Dispose();
                        if ((disposable = _disposable) == null) return;
                    }
                    try { disposable.Dispose(); } catch {/**/}
                    if (_error == null) _observer.OnCompleted();
                    else _observer.OnError(_error);
                }
            }

            private sealed class EmptyDisposable : IDisposable
            {
                public static EmptyDisposable Default { get; } = new EmptyDisposable();
                private EmptyDisposable() { }
                void IDisposable.Dispose() { }
            }
        }
    }
}
