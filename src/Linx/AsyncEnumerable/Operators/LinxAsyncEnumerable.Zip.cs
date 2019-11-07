namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current);
            }
        }

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae3 = source3.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current, ae3.Current);
            }
        }

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            Func<T1, T2, T3, T4, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae3 = source3.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae4 = source4.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current, ae3.Current, ae4.Current);
            }
        }

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae3 = source3.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae4 = source4.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae5 = source5.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current, ae3.Current, ae4.Current, ae5.Current);
            }
        }

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae3 = source3.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae4 = source4.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae5 = source5.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae6 = source6.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current, ae3.Current, ae4.Current, ae5.Current, ae6.Current);
            }
        }

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, T7, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));
            if (source7 == null) throw new ArgumentNullException(nameof(source7));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae3 = source3.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae4 = source4.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae5 = source5.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae6 = source6.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae7 = source7.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current, ae3.Current, ae4.Current, ae5.Current, ae6.Current, ae7.Current);
            }
        }

        /// <summary>
        /// Merges multiple sequences into one sequence by combining corresponding elements.
        /// </summary>
        public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            IAsyncEnumerable<T8> source8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));
            if (source7 == null) throw new ArgumentNullException(nameof(source7));
            if (source8 == null) throw new ArgumentNullException(nameof(source8));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(GetEnumerator);

            async IAsyncEnumerator<TResult> GetEnumerator(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();

                using var cts = new CancellationTokenSource();
                // ReSharper disable PossibleMultipleEnumeration
                await using var ae1 = source1.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae2 = source2.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae3 = source3.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae4 = source4.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae5 = source5.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae6 = source6.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae7 = source7.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                await using var ae8 = source8.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                // ReSharper restore PossibleMultipleEnumeration
                var ctx = new ZipContext(cts, ae1.MoveNextAsync, ae2.MoveNextAsync);
                using var _ = token.CanBeCanceled ? token.Register(() => ctx.SetError(new OperationCanceledException(token))) : default;

                while (await ctx.MoveNextAsync().ConfigureAwait(false))
                    yield return resultSelector(ae1.Current, ae2.Current, ae3.Current, ae4.Current, ae5.Current, ae6.Current, ae7.Current, ae8.Current);
            }
        }

    }
}
