namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Computes the sum of a sequence values.
        /// </summary>
        public static async Task<int> Sum(this IAsyncEnumerable<int> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var sum = 0;
                while (await ae.MoveNextAsync())
                    checked { sum += ae.Current; }
                return sum;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Computes the sum of a sequence of non-null values.
        /// </summary>
        public static Task<int> Sum(this IAsyncEnumerable<int?> source, CancellationToken token)
        {
            try { return source.Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<int>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<int> Sum<T>(this IAsyncEnumerable<T> source, Func<T, int> selector, CancellationToken token)
        {
            try { return source.Select(selector).Sum(token); }
            catch (Exception ex) { return Task.FromException<int>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of non-null values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<int> Sum<T>(this IAsyncEnumerable<T> source, Func<T, int?> selector, CancellationToken token)
        {
            try { return source.Select(selector).Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<int>(ex); }
        }

        /// <summary>
        /// Computes the sum of a sequence values.
        /// </summary>
        public static async Task<long> Sum(this IAsyncEnumerable<long> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var sum = 0L;
                while (await ae.MoveNextAsync())
                    checked { sum += ae.Current; }
                return sum;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Computes the sum of a sequence of non-null values.
        /// </summary>
        public static Task<long> Sum(this IAsyncEnumerable<long?> source, CancellationToken token)
        {
            try { return source.Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<long>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<long> Sum<T>(this IAsyncEnumerable<T> source, Func<T, long> selector, CancellationToken token)
        {
            try { return source.Select(selector).Sum(token); }
            catch (Exception ex) { return Task.FromException<long>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of non-null values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<long> Sum<T>(this IAsyncEnumerable<T> source, Func<T, long?> selector, CancellationToken token)
        {
            try { return source.Select(selector).Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<long>(ex); }
        }

        /// <summary>
        /// Computes the sum of a sequence values.
        /// </summary>
        public static async Task<double> Sum(this IAsyncEnumerable<double> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var sum = 0D;
                while (await ae.MoveNextAsync())
                    sum += ae.Current;
                return sum;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Computes the sum of a sequence of non-null values.
        /// </summary>
        public static Task<double> Sum(this IAsyncEnumerable<double?> source, CancellationToken token)
        {
            try { return source.Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<double>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<double> Sum<T>(this IAsyncEnumerable<T> source, Func<T, double> selector, CancellationToken token)
        {
            try { return source.Select(selector).Sum(token); }
            catch (Exception ex) { return Task.FromException<double>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of non-null values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<double> Sum<T>(this IAsyncEnumerable<T> source, Func<T, double?> selector, CancellationToken token)
        {
            try { return source.Select(selector).Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<double>(ex); }
        }

        /// <summary>
        /// Computes the sum of a sequence values.
        /// </summary>
        public static async Task<float> Sum(this IAsyncEnumerable<float> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var sum = 0F;
                while (await ae.MoveNextAsync())
                    sum += ae.Current;
                return sum;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Computes the sum of a sequence of non-null values.
        /// </summary>
        public static Task<float> Sum(this IAsyncEnumerable<float?> source, CancellationToken token)
        {
            try { return source.Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<float>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<float> Sum<T>(this IAsyncEnumerable<T> source, Func<T, float> selector, CancellationToken token)
        {
            try { return source.Select(selector).Sum(token); }
            catch (Exception ex) { return Task.FromException<float>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of non-null values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<float> Sum<T>(this IAsyncEnumerable<T> source, Func<T, float?> selector, CancellationToken token)
        {
            try { return source.Select(selector).Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<float>(ex); }
        }

        /// <summary>
        /// Computes the sum of a sequence values.
        /// </summary>
        public static async Task<decimal> Sum(this IAsyncEnumerable<decimal> source, CancellationToken token)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            token.ThrowIfCancellationRequested();

            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var sum = 0M;
                while (await ae.MoveNextAsync())
                    sum += ae.Current;
                return sum;
            }
            finally { await ae.DisposeAsync(); }
        }

        /// <summary>
        /// Computes the sum of a sequence of non-null values.
        /// </summary>
        public static Task<decimal> Sum(this IAsyncEnumerable<decimal?> source, CancellationToken token)
        {
            try { return source.Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<decimal>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<decimal> Sum<T>(this IAsyncEnumerable<T> source, Func<T, decimal> selector, CancellationToken token)
        {
            try { return source.Select(selector).Sum(token); }
            catch (Exception ex) { return Task.FromException<decimal>(ex); }
        }

        /// <summary>
        /// Computes the sum of the sequence of non-null values that are obtained by invoking a transform function on each element of the input sequence.
        /// </summary>
        public static Task<decimal> Sum<T>(this IAsyncEnumerable<T> source, Func<T, decimal?> selector, CancellationToken token)
        {
            try { return source.Select(selector).Values().Sum(token); }
            catch (Exception ex) { return Task.FromException<decimal>(ex); }
        }

    }
}
