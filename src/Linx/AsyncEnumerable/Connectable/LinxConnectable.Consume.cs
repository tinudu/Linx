using System;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxConnectable
{
    /// <summary>
    /// Aggregate <paramref name="source"/> by first calling the <paramref name="consumer"/> on a new <see cref="ISubject{T}"/>, then connecting it.
    /// </summary>
    public static ValueTask Consume<TSource>(
        this IConnectable<TSource> source,
        ConsumerDelegate<TSource> consumer,
        CancellationToken token)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (consumer is null) throw new ArgumentNullException(nameof(consumer));

        try
        {
            token.ThrowIfCancellationRequested();
            var subject = source.CreateSubject();
            var task = consumer(subject.AsyncEnumerable, token);
            subject.Connect();
            return task;
        }
        catch (Exception ex)
        {
            return new(Task.FromException(ex));
        }
    }
}
