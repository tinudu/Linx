namespace Linx.Reactive
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Invokes the specified action when the sequence terminates.
        /// </summary>
        public static IAsyncEnumerable<T> Finally<T>(this IAsyncEnumerable<T> source, Action @finally)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (@finally == null) throw new ArgumentNullException(nameof(@finally));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(ae.Current);
                }
                finally
                {
                    await ae.DisposeAsync();
                    @finally();
                }
            });

        }
    }
}
