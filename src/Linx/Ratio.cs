namespace Linx
{
    /// <summary>
    /// A ratio (sum and count).
    /// </summary>
    public struct Int64Ratio
    {
        /// <summary>
        /// The sum.
        /// </summary>
        public long Sum { get; }

        /// <summary>
        /// The count.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Gets the average.
        /// </summary>
        public double? Average() => Count != 0 ? (double)Sum / Count : default(double?);

        /// <summary>
        /// Initialize.
        /// </summary>
        public Int64Ratio(long sum, long count)
        {
            Sum = sum;
            Count = count;
        }

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static Int64Ratio operator +(Int64Ratio x, Int64Ratio y) => new Int64Ratio(checked(x.Sum + y.Sum), checked(x.Count + y.Count));

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static Int64Ratio operator +(Int64Ratio x, long y) => new Int64Ratio(checked(x.Sum + y), checked(x.Count + 1));

        /// <summary>
        /// (<see cref="Sum"/>/<see cref="Count"/>)
        /// </summary>
        public override string ToString() => $"({Sum}/{Count})";
    }

    /// <summary>
    /// A ratio (sum and count).
    /// </summary>
    public struct FloatRatio
    {
        /// <summary>
        /// The sum.
        /// </summary>
        public float Sum { get; }

        /// <summary>
        /// The count.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Gets the average.
        /// </summary>
        public float? Average() => Count != 0 ? Sum / Count : default(float?);

        /// <summary>
        /// Initialize.
        /// </summary>
        public FloatRatio(float sum, long count)
        {
            Sum = sum;
            Count = count;
        }

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static FloatRatio operator +(FloatRatio x, FloatRatio y) => new FloatRatio(x.Sum + y.Sum, checked(x.Count + y.Count));

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static FloatRatio operator +(FloatRatio x, float y) => new FloatRatio(x.Sum + y, checked(x.Count + 1));

        /// <summary>
        /// (<see cref="Sum"/>/<see cref="Count"/>)
        /// </summary>
        public override string ToString() => $"({Sum}/{Count})";
    }

    /// <summary>
    /// A ratio (sum and count).
    /// </summary>
    public struct DoubleRatio
    {
        /// <summary>
        /// The sum.
        /// </summary>
        public double Sum { get; }

        /// <summary>
        /// The count.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Gets the average.
        /// </summary>
        public double? Average() => Count != 0 ? Sum / Count : default(double?);

        /// <summary>
        /// Initialize.
        /// </summary>
        public DoubleRatio(double sum, long count)
        {
            Sum = sum;
            Count = count;
        }

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static DoubleRatio operator +(DoubleRatio x, DoubleRatio y) => new DoubleRatio(x.Sum + y.Sum, checked(x.Count + y.Count));

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static DoubleRatio operator +(DoubleRatio x, double y) => new DoubleRatio(x.Sum + y, checked(x.Count + 1));

        /// <summary>
        /// (<see cref="Sum"/>/<see cref="Count"/>)
        /// </summary>
        public override string ToString() => $"({Sum}/{Count})";
    }

    /// <summary>
    /// A ratio (sum and count).
    /// </summary>
    public struct DecimalRatio
    {
        /// <summary>
        /// The sum.
        /// </summary>
        public decimal Sum { get; }

        /// <summary>
        /// The count.
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Gets the average.
        /// </summary>
        public decimal? Average() => Count != 0 ? Sum / Count : default(decimal?);

        /// <summary>
        /// Initialize.
        /// </summary>
        public DecimalRatio(decimal sum, long count)
        {
            Sum = sum;
            Count = count;
        }

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static DecimalRatio operator +(DecimalRatio x, DecimalRatio y) => new DecimalRatio(x.Sum + y.Sum, checked(x.Count + y.Count));

        /// <summary>
        /// Addition operator.
        /// </summary>
        public static DecimalRatio operator +(DecimalRatio x, decimal y) => new DecimalRatio(x.Sum + y, checked(x.Count + 1));

        /// <summary>
        /// (<see cref="Sum"/>/<see cref="Count"/>)
        /// </summary>
        public override string ToString() => $"({Sum}/{Count})";
    }
}
