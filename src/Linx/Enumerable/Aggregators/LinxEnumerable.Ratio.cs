using System.Collections.Generic;
using System.Linq;

namespace Linx.Enumerable;

partial class LinxEnumerable
{
    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static Int64Ratio Ratio(this IEnumerable<int> source) => source.Aggregate(new Int64Ratio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static Int64Ratio Ratio(this IEnumerable<long> source) => source.Aggregate(new Int64Ratio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static Int64Ratio Ratio(this IEnumerable<Int64Ratio> source) => source.Aggregate(new Int64Ratio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static FloatRatio Ratio(this IEnumerable<float> source) => source.Aggregate(new FloatRatio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static FloatRatio Ratio(this IEnumerable<FloatRatio> source) => source.Aggregate(new FloatRatio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static DoubleRatio Ratio(this IEnumerable<double> source) => source.Aggregate(new DoubleRatio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static DoubleRatio Ratio(this IEnumerable<DoubleRatio> source) => source.Aggregate(new DoubleRatio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static DecimalRatio Ratio(this IEnumerable<decimal> source) => source.Aggregate(new DecimalRatio(), (a, c) => a + c);

    /// <summary>
    /// Ratio aggregation.
    /// </summary>
    public static DecimalRatio Ratio(this IEnumerable<DecimalRatio> source) => source.Aggregate(new DecimalRatio(), (a, c) => a + c);
}
