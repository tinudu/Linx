namespace Linx.Reactive
{
    using System;
    using System.Threading.Tasks;

    partial class LinxReactive
    {
        /// <summary>
        /// Gets a <see cref="IAsyncEnumerableObs{T}"/> that produces the specified value.
        /// </summary>
        public static IAsyncEnumerableObs<T> Return<T>(T value)
            => Produce<T>(async (yield, token) =>
            {
                await yield(value);
            });

        /// <summary>
        /// Gets a <see cref="IAsyncEnumerableObs{T}"/> that produces the value returned by the specified function.
        /// </summary>
        public static IAsyncEnumerableObs<T> Return<T>(Func<T> getValue)
            => Produce<T>(async (yield, token) =>
            {
                await yield(getValue());
            });

        /// <summary>
        /// Gets a <see cref="IAsyncEnumerableObs{T}"/> that produces the value returned by the specified async function.
        /// </summary>
        public static IAsyncEnumerableObs<T> Return<T>(Func<Task<T>> getValue)
            => Produce<T>(async (yield, token) =>
            {
                await yield(await getValue().ConfigureAwait(false));
            });
    }
}
