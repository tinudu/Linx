namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Function to yield sequence elements to a async enumerator.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>true if further elements are requested. false otherwise.</returns>
    /// <exception cref="System.OperationCanceledException">The enumerator <see cref="CancellationToken"/> requested cancellation.</exception>
    public delegate ValueTask<bool> YieldDelegate<in T>(T element);

    /// <summary>
    /// Coroutine to asynchronously produce sequence elements.
    /// </summary>
    /// <param name="yield"><see cref="YieldDelegate{T}"/> to which to yield elements.</param>
    /// <param name="token">Token on which cancellation is requested.</param>
    /// <returns>A task that completes when the sequence completed with or without error.</returns>
    public delegate Task ProducerDelegate<out T>(YieldDelegate<T> yield, CancellationToken token);

    /// <summary>
    /// Delegate to produce an aggregate from a sequence.
    /// </summary>
    public delegate Task<TAggregate> AggregatorDelegate<in TSource, TAggregate>(IAsyncEnumerable<TSource> source, CancellationToken token);

    /// <summary>
    /// Delegate to consume a sequence.
    /// </summary>
    public delegate Task ConsumerDelegate<in TSource>(IAsyncEnumerable<TSource> source, CancellationToken token);
}
