namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        public static IAsyncEnumerable<T> Finally<T>(this IAsyncEnumerable<T> source, Action @finally)
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
