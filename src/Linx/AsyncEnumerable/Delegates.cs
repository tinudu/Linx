namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Function that accepts sequence elements.
    /// </summary>
    public delegate ValueTask AcceptorDelegate<in T>(T value);

    /// <summary>
    /// Delegate to produce an aggregate from a sequence.
    /// </summary>
    public delegate Task<TAggregate> AggregatorDelegate<in TSource, TAggregate>(IAsyncEnumerable<TSource> source, CancellationToken token);

    /// <summary>
    /// Delegate to consume a sequence.
    /// </summary>
    public delegate Task ConsumerDelegate<in TSource>(IAsyncEnumerable<TSource> source, CancellationToken token);

    /// <summary>
    /// Coroutine to asynchronously produce sequence elements.
    /// </summary>
    /// <param name="yield"><see cref="AcceptorDelegate{T}"/> to which to yield elements.</param>
    /// <param name="token">Token on which cancellation is requested.</param>
    /// <returns>A task that completes when the sequence is ended, observes an error or acknowledges cancellation.</returns>
    public delegate Task ProducerDelegate<out T>(AcceptorDelegate<T> yield, CancellationToken token);

}
