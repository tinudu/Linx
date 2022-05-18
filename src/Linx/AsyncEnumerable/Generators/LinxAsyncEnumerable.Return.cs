using System.Collections.Generic;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Gets a <see cref="IAsyncEnumerable{T}"/> that produces the specified value.
    /// </summary>
    public static IAsyncEnumerable<T> Return<T>(T value)
    {
        return Iterator();

        async IAsyncEnumerable<T> Iterator()
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield return value;
        }
    }
}
