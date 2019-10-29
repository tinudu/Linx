namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<double?> Average(this IAsyncEnumerable<int> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<double?> Average(this IAsyncEnumerable<long> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<double?> Average(this IAsyncEnumerable<Int64Ratio> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<float?> Average(this IAsyncEnumerable<float> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<float?> Average(this IAsyncEnumerable<FloatRatio> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<double?> Average(this IAsyncEnumerable<double> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<double?> Average(this IAsyncEnumerable<DoubleRatio> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<decimal?> Average(this IAsyncEnumerable<decimal> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();

        /// <summary>
        /// Computes the average of a sequence.
        /// </summary>
        public static async Task<decimal?> Average(this IAsyncEnumerable<DecimalRatio> source, CancellationToken token) => (await source.Ratio(token).ConfigureAwait(false)).Average();
    }
}
