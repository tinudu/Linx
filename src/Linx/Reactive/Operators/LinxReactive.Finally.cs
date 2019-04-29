namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Invokes the specified action when the sequence terminates.
        /// </summary>
        public static IAsyncEnumerableObs<T> Finally<T>(this IAsyncEnumerableObs<T> source, Action @finally)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (@finally == null) throw new ArgumentNullException(nameof(@finally));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    while (await ae.MoveNextAsync())
                        await yield(ae.Current);
                }
                finally
                {
                    await ae.DisposeAsync().ConfigureAwait(false);
                    @finally();
                }
            });

        }
    }
}
