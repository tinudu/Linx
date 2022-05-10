namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges differently typed sequences into a <see cref="ValueTuple{T1, T2}"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values for any source that has not yet produced a value.
        /// </remarks>
        public static IAsyncEnumerable<ValueTuple<T1, T2>> Combine<T1, T2>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));

            return Defer(() =>
            {
                var tuple = new CombineTuple<T1, T2>(startAtFirstElement);
                return new[] 
                    {
                        source1.Select(v => tuple.OnNext1(v)),
                        source2.Select(v => tuple.OnNext2(v)),
                    }
                    .Merge()
                    .SkipUntil(t => t.HasValue)
                    .Select(t => t.GetValueOrDefault());
            });
        }

        private sealed class CombineTuple<T1, T2>
        {
            private int _missing;
            private T1 _value1 = default!;
            private T2 _value2 = default!;

            public CombineTuple(bool startAtFirstElement)
            {
                if (!startAtFirstElement)
                    _missing = (1 << 2) - 1;
            }

            public ValueTuple<T1, T2>? OnNext1(T1 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2>(_value1, _value2): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2>? OnNext2(T2 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2>(_value1, _value2): default;
                _missing = missing;
                return result;
            }

        }

        /// <summary>
        /// Merges differently typed sequences into a <see cref="ValueTuple{T1, T2, T3}"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values for any source that has not yet produced a value.
        /// </remarks>
        public static IAsyncEnumerable<ValueTuple<T1, T2, T3>> Combine<T1, T2, T3>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));

            return Defer(() =>
            {
                var tuple = new CombineTuple<T1, T2, T3>(startAtFirstElement);
                return new[] 
                    {
                        source1.Select(v => tuple.OnNext1(v)),
                        source2.Select(v => tuple.OnNext2(v)),
                        source3.Select(v => tuple.OnNext3(v)),
                    }
                    .Merge()
                    .SkipUntil(t => t.HasValue)
                    .Select(t => t.GetValueOrDefault());
            });
        }

        private sealed class CombineTuple<T1, T2, T3>
        {
            private int _missing;
            private T1 _value1 = default!;
            private T2 _value2 = default!;
            private T3 _value3 = default!;

            public CombineTuple(bool startAtFirstElement)
            {
                if (!startAtFirstElement)
                    _missing = (1 << 3) - 1;
            }

            public ValueTuple<T1, T2, T3>? OnNext1(T1 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3>(_value1, _value2, _value3): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3>? OnNext2(T2 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3>(_value1, _value2, _value3): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3>? OnNext3(T3 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3>(_value1, _value2, _value3): default;
                _missing = missing;
                return result;
            }

        }

        /// <summary>
        /// Merges differently typed sequences into a <see cref="ValueTuple{T1, T2, T3, T4}"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values for any source that has not yet produced a value.
        /// </remarks>
        public static IAsyncEnumerable<ValueTuple<T1, T2, T3, T4>> Combine<T1, T2, T3, T4>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));

            return Defer(() =>
            {
                var tuple = new CombineTuple<T1, T2, T3, T4>(startAtFirstElement);
                return new[] 
                    {
                        source1.Select(v => tuple.OnNext1(v)),
                        source2.Select(v => tuple.OnNext2(v)),
                        source3.Select(v => tuple.OnNext3(v)),
                        source4.Select(v => tuple.OnNext4(v)),
                    }
                    .Merge()
                    .SkipUntil(t => t.HasValue)
                    .Select(t => t.GetValueOrDefault());
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4>
        {
            private int _missing;
            private T1 _value1 = default!;
            private T2 _value2 = default!;
            private T3 _value3 = default!;
            private T4 _value4 = default!;

            public CombineTuple(bool startAtFirstElement)
            {
                if (!startAtFirstElement)
                    _missing = (1 << 4) - 1;
            }

            public ValueTuple<T1, T2, T3, T4>? OnNext1(T1 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4>(_value1, _value2, _value3, _value4): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4>? OnNext2(T2 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4>(_value1, _value2, _value3, _value4): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4>? OnNext3(T3 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4>(_value1, _value2, _value3, _value4): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4>? OnNext4(T4 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4>(_value1, _value2, _value3, _value4): default;
                _missing = missing;
                return result;
            }

        }

        /// <summary>
        /// Merges differently typed sequences into a <see cref="ValueTuple{T1, T2, T3, T4, T5}"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values for any source that has not yet produced a value.
        /// </remarks>
        public static IAsyncEnumerable<ValueTuple<T1, T2, T3, T4, T5>> Combine<T1, T2, T3, T4, T5>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));

            return Defer(() =>
            {
                var tuple = new CombineTuple<T1, T2, T3, T4, T5>(startAtFirstElement);
                return new[] 
                    {
                        source1.Select(v => tuple.OnNext1(v)),
                        source2.Select(v => tuple.OnNext2(v)),
                        source3.Select(v => tuple.OnNext3(v)),
                        source4.Select(v => tuple.OnNext4(v)),
                        source5.Select(v => tuple.OnNext5(v)),
                    }
                    .Merge()
                    .SkipUntil(t => t.HasValue)
                    .Select(t => t.GetValueOrDefault());
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5>
        {
            private int _missing;
            private T1 _value1 = default!;
            private T2 _value2 = default!;
            private T3 _value3 = default!;
            private T4 _value4 = default!;
            private T5 _value5 = default!;

            public CombineTuple(bool startAtFirstElement)
            {
                if (!startAtFirstElement)
                    _missing = (1 << 5) - 1;
            }

            public ValueTuple<T1, T2, T3, T4, T5>? OnNext1(T1 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5>(_value1, _value2, _value3, _value4, _value5): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5>? OnNext2(T2 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5>(_value1, _value2, _value3, _value4, _value5): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5>? OnNext3(T3 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5>(_value1, _value2, _value3, _value4, _value5): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5>? OnNext4(T4 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5>(_value1, _value2, _value3, _value4, _value5): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5>? OnNext5(T5 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5>(_value1, _value2, _value3, _value4, _value5): default;
                _missing = missing;
                return result;
            }

        }

        /// <summary>
        /// Merges differently typed sequences into a <see cref="ValueTuple{T1, T2, T3, T4, T5, T6}"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values for any source that has not yet produced a value.
        /// </remarks>
        public static IAsyncEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6>> Combine<T1, T2, T3, T4, T5, T6>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));

            return Defer(() =>
            {
                var tuple = new CombineTuple<T1, T2, T3, T4, T5, T6>(startAtFirstElement);
                return new[] 
                    {
                        source1.Select(v => tuple.OnNext1(v)),
                        source2.Select(v => tuple.OnNext2(v)),
                        source3.Select(v => tuple.OnNext3(v)),
                        source4.Select(v => tuple.OnNext4(v)),
                        source5.Select(v => tuple.OnNext5(v)),
                        source6.Select(v => tuple.OnNext6(v)),
                    }
                    .Merge()
                    .SkipUntil(t => t.HasValue)
                    .Select(t => t.GetValueOrDefault());
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5, T6>
        {
            private int _missing;
            private T1 _value1 = default!;
            private T2 _value2 = default!;
            private T3 _value3 = default!;
            private T4 _value4 = default!;
            private T5 _value5 = default!;
            private T6 _value6 = default!;

            public CombineTuple(bool startAtFirstElement)
            {
                if (!startAtFirstElement)
                    _missing = (1 << 6) - 1;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6>? OnNext1(T1 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6>(_value1, _value2, _value3, _value4, _value5, _value6): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6>? OnNext2(T2 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6>(_value1, _value2, _value3, _value4, _value5, _value6): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6>? OnNext3(T3 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6>(_value1, _value2, _value3, _value4, _value5, _value6): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6>? OnNext4(T4 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6>(_value1, _value2, _value3, _value4, _value5, _value6): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6>? OnNext5(T5 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6>(_value1, _value2, _value3, _value4, _value5, _value6): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6>? OnNext6(T6 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 5);
                _value6 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6>(_value1, _value2, _value3, _value4, _value5, _value6): default;
                _missing = missing;
                return result;
            }

        }

        /// <summary>
        /// Merges differently typed sequences into a <see cref="ValueTuple{T1, T2, T3, T4, T5, T6, T7}"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values for any source that has not yet produced a value.
        /// </remarks>
        public static IAsyncEnumerable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>> Combine<T1, T2, T3, T4, T5, T6, T7>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));
            if (source7 == null) throw new ArgumentNullException(nameof(source7));

            return Defer(() =>
            {
                var tuple = new CombineTuple<T1, T2, T3, T4, T5, T6, T7>(startAtFirstElement);
                return new[] 
                    {
                        source1.Select(v => tuple.OnNext1(v)),
                        source2.Select(v => tuple.OnNext2(v)),
                        source3.Select(v => tuple.OnNext3(v)),
                        source4.Select(v => tuple.OnNext4(v)),
                        source5.Select(v => tuple.OnNext5(v)),
                        source6.Select(v => tuple.OnNext6(v)),
                        source7.Select(v => tuple.OnNext7(v)),
                    }
                    .Merge()
                    .SkipUntil(t => t.HasValue)
                    .Select(t => t.GetValueOrDefault());
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5, T6, T7>
        {
            private int _missing;
            private T1 _value1 = default!;
            private T2 _value2 = default!;
            private T3 _value3 = default!;
            private T4 _value4 = default!;
            private T5 _value5 = default!;
            private T6 _value6 = default!;
            private T7 _value7 = default!;

            public CombineTuple(bool startAtFirstElement)
            {
                if (!startAtFirstElement)
                    _missing = (1 << 7) - 1;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext1(T1 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext2(T2 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext3(T3 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext4(T4 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext5(T5 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext6(T6 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 5);
                _value6 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

            public ValueTuple<T1, T2, T3, T4, T5, T6, T7>? OnNext7(T7 value)
            {
                var missing = Atomic.Lock(ref _missing) & ~(1 << 6);
                _value7 = value;
                var result = missing == 0 ? new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(_value1, _value2, _value3, _value4, _value5, _value6, _value7): default;
                _missing = missing;
                return result;
            }

        }

    }
}