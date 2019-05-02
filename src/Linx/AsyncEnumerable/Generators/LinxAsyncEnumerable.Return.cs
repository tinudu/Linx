﻿namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that produces the specified value.
        /// </summary>
        public static IAsyncEnumerable<T> Return<T>(T value)
            => Produce<T>(async (yield, token) =>
            {
                await yield(value);
            });

        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that produces the value returned by the specified function.
        /// </summary>
        public static IAsyncEnumerable<T> Return<T>(Func<T> getValue)
            => Produce<T>(async (yield, token) =>
            {
                await yield(getValue());
            });

        /// <summary>
        /// Gets a <see cref="IAsyncEnumerable{T}"/> that produces the value returned by the specified async function.
        /// </summary>
        public static IAsyncEnumerable<T> Return<T>(Func<Task<T>> getValue)
            => Produce<T>(async (yield, token) =>
            {
                await yield(await getValue().ConfigureAwait(false));
            });
    }
}