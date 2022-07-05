using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<Int64Ratio> Ratio(this IAsyncEnumerable<int> source, CancellationToken token) => source.Aggregate(new Int64Ratio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<Int64Ratio> Ratio(this IAsyncEnumerable<long> source, CancellationToken token) => source.Aggregate(new Int64Ratio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<Int64Ratio> Ratio(this IAsyncEnumerable<Int64Ratio> source, CancellationToken token) => source.Aggregate(new Int64Ratio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<FloatRatio> Ratio(this IAsyncEnumerable<float> source, CancellationToken token) => source.Aggregate(new FloatRatio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<FloatRatio> Ratio(this IAsyncEnumerable<FloatRatio> source, CancellationToken token) => source.Aggregate(new FloatRatio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<DoubleRatio> Ratio(this IAsyncEnumerable<double> source, CancellationToken token) => source.Aggregate(new DoubleRatio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<DoubleRatio> Ratio(this IAsyncEnumerable<DoubleRatio> source, CancellationToken token) => source.Aggregate(new DoubleRatio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<DecimalRatio> Ratio(this IAsyncEnumerable<decimal> source, CancellationToken token) => source.Aggregate(new DecimalRatio(), (a, c) => a + c, token);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static ValueTask<DecimalRatio> Ratio(this IAsyncEnumerable<DecimalRatio> source, CancellationToken token) => source.Aggregate(new DecimalRatio(), (a, c) => a + c, token);
}
