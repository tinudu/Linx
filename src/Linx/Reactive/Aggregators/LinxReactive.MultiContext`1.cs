namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Subjects;

    partial class LinxReactive
    {
        /// <summary>
        /// Multiple consumers/aggregators per subscription.
        /// </summary>
        private sealed class MultiContext<T>
        {
            private readonly ColdSubject<T> _subject;
            private ErrorHandler _eh = ErrorHandler.Init();
            private int _state; // 1: canceled

            /// <summary>
            /// Initialize.
            /// </summary>
            /// <param name="capacity">Expected number of subscribers.</param>
            /// <param name="token">Token to cancel the whole operation.</param>
            public MultiContext(int capacity, CancellationToken token)
            {
                _subject = new ColdSubject<T>(capacity);
                if (token.CanBeCanceled) _eh.ExternalRegistration = token.Register(() =>
                {
                    var state = Atomic.Lock(ref _state);
                    if (state == 0)
                    {
                        _eh.SetExternalError(new OperationCanceledException(token));
                        _state = 1;
                        _eh.Cancel();
                    }
                    else
                        _state = 1;
                });
            }

            public async Task<TAggregate> Aggregate<TAggregate>(AggregatorDelegate<T, TAggregate> aggregator)
            {
                _eh.InternalToken.ThrowIfCancellationRequested();
                try { return await aggregator(_subject.Output, _eh.InternalToken).ConfigureAwait(false); }
                catch (Exception ex) { HandleError(ex); throw; }
            }

            public async Task Consume(ConsumerDelegate<T> consumer)
            {
                _eh.InternalToken.ThrowIfCancellationRequested();
                try { await consumer(_subject.Output, _eh.InternalToken).ConfigureAwait(false); }
                catch (Exception ex) { HandleError(ex); throw; }
            }

            public async Task SubscribeTo(IAsyncEnumerable<T> source)
            {
                _eh.InternalToken.ThrowIfCancellationRequested();
                try { await _subject.SubscribeTo(source).ConfigureAwait(false); }
                catch (Exception ex) { HandleError(ex); throw; }
            }

            public async Task WhenAll(params Task[] tasks)
            {
                try { await Task.WhenAll(tasks).ConfigureAwait(false); }
                catch (Exception ex) { HandleError(ex); }
                if (Atomic.TestAndSet(ref _state, 0, 1) == 0) _eh.Cancel();
                _eh.ThrowIfError();
            }

            private void HandleError(Exception error)
            {
                var state = Atomic.Lock(ref _state);
                _eh.SetInternalError(error);
                _state = 1;
                if (state == 0) _eh.Cancel();
            }
        }
    }
}
