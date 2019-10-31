namespace Linx.Enumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AsyncEnumerable;

    partial class LinxEnumerable
    {
        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                    yield return await selector(item, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Projects each element of a sequence into a new form.
        /// </summary>
        public static IAsyncEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task<TResult>> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            return LinxAsyncEnumerable.Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                var i = 0;
                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in source)
                    yield return await selector(item, i++, token).ConfigureAwait(false);
            }
        }
    }
}
