namespace Linx.Reactive
{
    using System;

    partial class LinxReactive
    {
        /// <summary>
        /// Returns the elements of the specified sequence or the type parameter's default value in a singleton sequence if the sequence is empty.
        /// </summary>
        public static IAsyncEnumerableObs<T> DefaultIfEmpty<T>(this IAsyncEnumerableObs<T> source) => source.DefaultIfEmpty(default);

        /// <summary>
        /// Returns the elements of the specified sequence or the specified <paramref name="default"/>, if the sequence is empty.
        /// </summary>
        public static IAsyncEnumerableObs<T> DefaultIfEmpty<T>(this IAsyncEnumerableObs<T> source, T @default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var ae = source.GetAsyncEnumerator(token);
                try
                {
                    if (!await ae.MoveNextAsync())
                        await yield(@default);
                    else
                    {
                        await yield(ae.Current);
                        while (await ae.MoveNextAsync())
                            await yield(ae.Current);
                    }
                }
                finally { await ae.DisposeAsync(); }
            });
        }
    }
}
