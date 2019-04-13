namespace Linx.Reactive
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the element at a specified index in a sequence.
        /// </summary>
        public static async Task<T> ElementAt<T>(this IAsyncEnumerable<T> source, int index, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (index < 0) throw new IndexOutOfRangeException();

            token.ThrowIfCancellationRequested();
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var i = 0;
                while (await ae.MoveNextAsync())
                    if (i++ == index)
                        return ae.Current;

                throw new IndexOutOfRangeException();
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
