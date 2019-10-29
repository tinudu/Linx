namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                return new[] { seq1, seq2 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, TResult>
        {
            private readonly Func<T1, T2, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;

            public CombineTuple(Func<T1, T2, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 2) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2)); }
                finally { _missing = 0; }
            }
        }
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, T3, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, T3, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                var seq3 = source3.Select(v => tuple.OnNext3(v));
                return new[] { seq1, seq2, seq3 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, T3, TResult>
        {
            private readonly Func<T1, T2, T3, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;
            private T3 _value3;

            public CombineTuple(Func<T1, T2, T3, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 3) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext3(T3 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2, _value3)); }
                finally { _missing = 0; }
            }
        }
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, T3, T4, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            Func<T1, T2, T3, T4, TResult> resultSelector,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, T3, T4, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                var seq3 = source3.Select(v => tuple.OnNext3(v));
                var seq4 = source4.Select(v => tuple.OnNext4(v));
                return new[] { seq1, seq2, seq3, seq4 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, TResult>
        {
            private readonly Func<T1, T2, T3, T4, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;
            private T3 _value3;
            private T4 _value4;

            public CombineTuple(Func<T1, T2, T3, T4, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 4) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext3(T3 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext4(T4 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2, _value3, _value4)); }
                finally { _missing = 0; }
            }
        }
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, T3, T4, T5, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, T3, T4, T5, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                var seq3 = source3.Select(v => tuple.OnNext3(v));
                var seq4 = source4.Select(v => tuple.OnNext4(v));
                var seq5 = source5.Select(v => tuple.OnNext5(v));
                return new[] { seq1, seq2, seq3, seq4, seq5 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5, TResult>
        {
            private readonly Func<T1, T2, T3, T4, T5, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;
            private T3 _value3;
            private T4 _value4;
            private T5 _value5;

            public CombineTuple(Func<T1, T2, T3, T4, T5, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 5) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext3(T3 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext4(T4 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext5(T5 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2, _value3, _value4, _value5)); }
                finally { _missing = 0; }
            }
        }
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, T3, T4, T5, T6, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, T3, T4, T5, T6, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                var seq3 = source3.Select(v => tuple.OnNext3(v));
                var seq4 = source4.Select(v => tuple.OnNext4(v));
                var seq5 = source5.Select(v => tuple.OnNext5(v));
                var seq6 = source6.Select(v => tuple.OnNext6(v));
                return new[] { seq1, seq2, seq3, seq4, seq5, seq6 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5, T6, TResult>
        {
            private readonly Func<T1, T2, T3, T4, T5, T6, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;
            private T3 _value3;
            private T4 _value4;
            private T5 _value5;
            private T6 _value6;

            public CombineTuple(Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 6) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext3(T3 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext4(T4 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext5(T5 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext6(T6 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 5);
                _value6 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2, _value3, _value4, _value5, _value6)); }
                finally { _missing = 0; }
            }
        }
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, T3, T4, T5, T6, T7, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector,
            bool startAtFirstElement = false)
        {
            if (source1 == null) throw new ArgumentNullException(nameof(source1));
            if (source2 == null) throw new ArgumentNullException(nameof(source2));
            if (source3 == null) throw new ArgumentNullException(nameof(source3));
            if (source4 == null) throw new ArgumentNullException(nameof(source4));
            if (source5 == null) throw new ArgumentNullException(nameof(source5));
            if (source6 == null) throw new ArgumentNullException(nameof(source6));
            if (source7 == null) throw new ArgumentNullException(nameof(source7));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, T3, T4, T5, T6, T7, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                var seq3 = source3.Select(v => tuple.OnNext3(v));
                var seq4 = source4.Select(v => tuple.OnNext4(v));
                var seq5 = source5.Select(v => tuple.OnNext5(v));
                var seq6 = source6.Select(v => tuple.OnNext6(v));
                var seq7 = source7.Select(v => tuple.OnNext7(v));
                return new[] { seq1, seq2, seq3, seq4, seq5, seq6, seq7 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5, T6, T7, TResult>
        {
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;
            private T3 _value3;
            private T4 _value4;
            private T5 _value5;
            private T6 _value6;
            private T7 _value7;

            public CombineTuple(Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 7) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext3(T3 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext4(T4 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext5(T5 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext6(T6 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 5);
                _value6 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext7(T7 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 6);
                _value7 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2, _value3, _value4, _value5, _value6, _value7)); }
                finally { _missing = 0; }
            }
        }
        /// <summary>
        /// Merges differently typed sequences into one.
        /// </summary>
        /// <remarks>
        /// <paramref name="startAtFirstElement"/> determines how the start of the sequence is handled.
        /// If it's false (the default), the operator only produces elements once all sources have produced their first element.
        /// If it's true, the sequence starts at the first element produced by any source,
        /// with default values passed to the <paramref name="resultSelector"/> for any source that has not produced a value.
        /// </remarks>
        public static IAsyncEnumerable<TResult> Combine<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(this
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            IAsyncEnumerable<T8> source8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector,
            bool startAtFirstElement = false)
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

            return Create(token =>
            {
                token.ThrowIfCancellationRequested();
                var tuple = new CombineTuple<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(resultSelector, startAtFirstElement);
                var seq1 = source1.Select(v => tuple.OnNext1(v));
                var seq2 = source2.Select(v => tuple.OnNext2(v));
                var seq3 = source3.Select(v => tuple.OnNext3(v));
                var seq4 = source4.Select(v => tuple.OnNext4(v));
                var seq5 = source5.Select(v => tuple.OnNext5(v));
                var seq6 = source6.Select(v => tuple.OnNext6(v));
                var seq7 = source7.Select(v => tuple.OnNext7(v));
                var seq8 = source8.Select(v => tuple.OnNext8(v));
                return new[] { seq1, seq2, seq3, seq4, seq5, seq6, seq7, seq8 }.Merge().SkipUntil(m => m.HasValue).Select(m => m.Value).GetAsyncEnumerator(token);
            });
        }

        private sealed class CombineTuple<T1, T2, T3, T4, T5, T6, T7, T8, TResult>
        {
            private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _resultSelector;
            private int _missing;
            private T1 _value1;
            private T2 _value2;
            private T3 _value3;
            private T4 _value4;
            private T5 _value5;
            private T6 _value6;
            private T7 _value7;
            private T8 _value8;

            public CombineTuple(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector, bool startAtFirstElement = true)
            {
                _resultSelector = resultSelector;
                if (!startAtFirstElement) _missing = (1 << 8) - 1;
            }

            public (bool HasValue, TResult Value) OnNext1(T1 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 0);
                _value1 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext2(T2 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 1);
                _value2 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext3(T3 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 2);
                _value3 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext4(T4 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 3);
                _value4 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext5(T5 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 4);
                _value5 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext6(T6 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 5);
                _value6 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext7(T7 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 6);
                _value7 = value;
                return GetResult(missing);
            }

            public (bool HasValue, TResult Value) OnNext8(T8 value)
            {
                // ReSharper disable once ShiftExpressionRealShiftCountIsZero
                var missing = Atomic.Lock(ref _missing) & ~(1 << 7);
                _value8 = value;
                return GetResult(missing);
            }

            private (bool HasValue, TResult Value) GetResult(int missing)
            {
                Debug.Assert((_missing & Atomic.LockBit) != 0);
                if (missing != 0) { _missing = missing; return default; }
                try { return (true, _resultSelector(_value1, _value2, _value3, _value4, _value5, _value6, _value7, _value8)); }
                finally { _missing = 0; }
            }
        }
    }
}