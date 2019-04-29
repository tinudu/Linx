namespace Linx.Reactive.Subjects
{
    using System.Threading.Tasks;

    /// <summary>
    /// A subject.
    /// </summary>
    public interface ISubject<in TSource, out TResult>
    {
        /// <summary>
        /// The output sequence.
        /// </summary>
        IAsyncEnumerableObs<TResult> Output { get; }

        /// <summary>
        /// Subscribe to the specified input source.
        /// </summary>
        /// <returns>A task that completes when all subscribers are unsubscribed.</returns>
        /// <exception cref="System.InvalidOperationException">Already subscribed.</exception>
        Task SubscribeTo(IAsyncEnumerableObs<TSource> input);
    }
}
