namespace Linx.Jsxn.TypeSystem
{
    using System.Collections.Generic;

    /// <summary>
    /// Base class for all primitive types.
    /// </summary>
    public abstract class PrimitiveType : NonNullableType, INamedType
    {
        /// <summary>
        /// Gets the predefined <see cref="PrimitiveType"/>s.
        /// </summary>
        public static IList<PrimitiveType> Predefined { get; } = new List<PrimitiveType>
        {
            BooleanType.Instance,
            IntType.Instance,
            LongType.Instance,
            FloatType.Instance,
            DoubleType.Instance,
            DecimalType.Instance,
            StringType.Instance,
            GuidType.Instance,
            DateTimeType.Instance,
            DateTimeOffsetType.Instance,
            TimeSpanType.Instance,
            BinaryType.Instance,
        }.AsReadOnly();

        /// <summary>
        /// The name.
        /// </summary>
        public Identifier Name { get; }

        internal PrimitiveType(Identifier name) => Name = name;

        /// <summary>
        /// <see cref="Name"/>.
        /// </summary>
        public sealed override string ToString() => Name;
    }

    /// <summary>
    /// "bool" type.
    /// </summary>
    public sealed class BooleanType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static BooleanType Instance = new BooleanType();
        private BooleanType() : base((Identifier)"bool") { }
    }

    /// <summary>
    /// "int" type.
    /// </summary>
    public sealed class IntType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static IntType Instance = new IntType();
        private IntType() : base((Identifier)"int") { }
    }

    /// <summary>
    /// "long" type.
    /// </summary>
    public sealed class LongType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static LongType Instance = new LongType();
        private LongType() : base((Identifier)"long") { }
    }

    /// <summary>
    /// "float" type.
    /// </summary>
    public sealed class FloatType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static FloatType Instance = new FloatType();
        private FloatType() : base((Identifier)"float") { }
    }

    /// <summary>
    /// "double" type.
    /// </summary>
    public sealed class DoubleType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static DoubleType Instance = new DoubleType();
        private DoubleType() : base((Identifier)"double") { }
    }

    /// <summary>
    /// "decimal" type.
    /// </summary>
    public sealed class DecimalType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static DecimalType Instance = new DecimalType();
        private DecimalType() : base((Identifier)"decimal") { }
    }

    /// <summary>
    /// "string" type.
    /// </summary>
    public sealed class StringType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static StringType Instance = new StringType();
        private StringType() : base((Identifier)"string") { }
    }

    /// <summary>
    /// "guid" type.
    /// </summary>
    public sealed class GuidType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static GuidType Instance = new GuidType();
        private GuidType() : base((Identifier)"guid") { }
    }

    /// <summary>
    /// "dt" type.
    /// </summary>
    public sealed class DateTimeType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static DateTimeType Instance = new DateTimeType();
        private DateTimeType() : base((Identifier)"dt") { }
    }

    /// <summary>
    /// "dto" type.
    /// </summary>
    public sealed class DateTimeOffsetType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static DateTimeOffsetType Instance = new DateTimeOffsetType();
        private DateTimeOffsetType() : base((Identifier)"dto") { }
    }

    /// <summary>
    /// "ts" type.
    /// </summary>
    public sealed class TimeSpanType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static TimeSpanType Instance = new TimeSpanType();
        private TimeSpanType() : base((Identifier)"ts") { }
    }

    /// <summary>
    /// "binary" type.
    /// </summary>
    public sealed class BinaryType : PrimitiveType
    {
        /// <summary>
        /// Singleton.
        /// </summary>
        public static BinaryType Instance = new BinaryType();
        private BinaryType() : base((Identifier)"binary") { }
    }

}