using System;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxConnectable
{
    /// <summary>
    /// Aggregate <paramref name="source"/> by first calling the <paramref name="aggregator"/> on a new <see cref="ISubject{T}"/>, then connecting it.
    /// </summary>
    public static ValueTask<TResult> Aggregate<TSource, TResult>(
        this IConnectable<TSource> source,
        AggregatorDelegate<TSource, TResult> aggregator,
        CancellationToken token)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (aggregator is null) throw new ArgumentNullException(nameof(aggregator));

        try
        {
            token.ThrowIfCancellationRequested();
            var subject = source.CreateSubject();
            var task = aggregator(subject.AsyncEnumerable, token);
            subject.Connect();
            return task;
        }
        catch (Exception ex)
        {
            return new(Task.FromException<TResult>(ex));
        }
    }
}
