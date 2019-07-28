namespace Linx.Observable
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    partial class LinxObservable
    {
        /// <summary>
        /// Convert a <see cref="IObservable{T}"/> to a <see cref="ILinxObservable{T}"/>.
        /// </summary>
        public static ILinxObservable<T> ToLinxObservable<T>(this IObservable<T> source)
            => new ObservableToLinxObservable<T>(source);

        private sealed class ObservableToLinxObservable<T> : ILinxObservable<T>
        {
            private readonly IObservable<T> _source;
            public ObservableToLinxObservable(IObservable<T> source) => _source = source ?? throw new ArgumentNullException(nameof(source));

            public void Subscribe(ILinxObserver<T> observer)
            {
                if (observer == null) throw new ArgumentNullException(nameof(observer));

                var subscription = new Subscription(observer);
                subscription.SetDisposable(_source.Subscribe(subscription));
            }

            private sealed class Subscription : IObserver<T>
            {
                private enum State { Subscribed, Completed, Disposed }

                private readonly object _gate = new object();
                private readonly ILinxObserver<T> _observer;
                private CancellationTokenRegistration _ctr;
                private State _state;
                private IDisposable _disposable;
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
                        if (_state != State.Subscribed)
                            return;
                        try { if (!_observer.OnNext(value)) Complete(null); }
                        catch (Exception ex) { Complete(ex); }
                    }
                }

                public void OnError(Exception error)
                {
                    if (error == null) throw new ArgumentNullException(nameof(error));
                    Complete(error);
                }

                public void OnCompleted() => Complete(null);

                public void SetDisposable(IDisposable disposable)
                {
                    lock (_gate)
                        if (_state == State.Subscribed)
                            _disposable = disposable ?? EmptyDisposable.Default;
                        else
                        {
                            Debug.Assert(_state == State.Completed);
                            try { disposable?.Dispose(); } catch {/**/}
                            _state = State.Disposed;
                            if (_error == null) _observer.OnCompleted();
                            else _observer.OnError(_error);
                        }
                }

                private void Complete(Exception error)
                {
                    lock (_gate)
                    {
                        if (_state != State.Subscribed)
                            return;

                        _error = error;
                        _ctr.Dispose();
                        var disposable = Linx.Clear(ref _disposable);
                        if (disposable == null)
                            _state = State.Completed;
                        else
                        {
                            try { disposable.Dispose(); } catch { /**/ }
                            _state = State.Disposed;
                            if (_error == null) _observer.OnCompleted();
                            else _observer.OnError(_error);
                        }
                    }
                }
            }

            private sealed class EmptyDisposable : IDisposable
            {
                public static EmptyDisposable Default { get; } = new EmptyDisposable();
                private EmptyDisposable() { }
                public void Dispose() { }
            }
        }
    }
}
