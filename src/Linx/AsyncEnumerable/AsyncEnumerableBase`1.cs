namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Observable;

    /// <summary>
    /// Base class for <see cref="IAsyncEnumerable{T}"/>, which also implements <see cref="ILinxObservable{T}"/>.
    /// </summary>
    public abstract class AsyncEnumerableBase<T> : IAsyncEnumerable<T>, ILinxObservable<T>
    {
        /// <inheritdoc />
        public abstract IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken token);

        /// <inheritdoc />
        public async void Subscribe(ILinxObserver<T> observer)
        {
            if (observer == null) throw new ArgumentNullException(nameof(observer));

            try
            {
                observer.Token.ThrowIfCancellationRequested();
                var ae = this.WithCancellation(observer.Token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                        if (!observer.OnNext(ae.Current))
                            break;
                }
                finally { await ae.DisposeAsync(); }
                observer.OnCompleted();
            }
            catch (Exception ex) { observer.OnError(ex); }
        }

        /// <inheritdoc />
        public abstract override string ToString();
    }
}
