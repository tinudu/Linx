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
        IAsyncEnumerable<TResult> Sink { get; }

        /// <summary>
        /// Subscribe to the specified source.
        /// </summary>
        /// <param name="source"></param>
        Task SubscribeTo(IAsyncEnumerable<TSource> source);
    }
}
