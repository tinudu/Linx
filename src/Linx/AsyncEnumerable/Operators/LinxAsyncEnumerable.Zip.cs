using System;
using System.Collections.Generic;

namespace Linx.AsyncEnumerable;

partial class LinxAsyncEnumerable
{
    /// <summary>
    /// Merges multiple sequences into one sequence by combining corresponding elements.
    /// </summary>
    public static IAsyncEnumerable<TResult> Zip<T1, T2, TResult>(this
        IAsyncEnumerable<T1> source1,
        IAsyncEnumerable<T2> source2,
        Func<T1, T2, TResult> resultSelector)
        => new ZipIterator<T1, T2, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

    /// <summary>
    /// Merges multiple sequences into one sequence by combining corresponding elements.
    /// </summary>
    public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, TResult>(this
        IAsyncEnumerable<T1> source1,
        IAsyncEnumerable<T2> source2,
        IAsyncEnumerable<T3> source3,
        Func<T1, T2, T3, TResult> resultSelector)
        => new ZipIterator<T1, T2, T3, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            source3 ?? throw new ArgumentNullException(nameof(source3)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

    /// <summary>
    /// Merges multiple sequences into one sequence by combining corresponding elements.
    /// </summary>
    public static IAsyncEnumerable<TResult> Zip<T1, T2, T3, T4, TResult>(this
        IAsyncEnumerable<T1> source1,
        IAsyncEnumerable<T2> source2,
        IAsyncEnumerable<T3> source3,
        IAsyncEnumerable<T4> source4,
        Func<T1, T2, T3, T4, TResult> resultSelector)
        => new ZipIterator<T1, T2, T3, T4, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            source3 ?? throw new ArgumentNullException(nameof(source3)),
            source4 ?? throw new ArgumentNullException(nameof(source4)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

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
        => new ZipIterator<T1, T2, T3, T4, T5, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            source3 ?? throw new ArgumentNullException(nameof(source3)),
            source4 ?? throw new ArgumentNullException(nameof(source4)),
            source5 ?? throw new ArgumentNullException(nameof(source5)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

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
        => new ZipIterator<T1, T2, T3, T4, T5, T6, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            source3 ?? throw new ArgumentNullException(nameof(source3)),
            source4 ?? throw new ArgumentNullException(nameof(source4)),
            source5 ?? throw new ArgumentNullException(nameof(source5)),
            source6 ?? throw new ArgumentNullException(nameof(source6)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

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
        => new ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            source3 ?? throw new ArgumentNullException(nameof(source3)),
            source4 ?? throw new ArgumentNullException(nameof(source4)),
            source5 ?? throw new ArgumentNullException(nameof(source5)),
            source6 ?? throw new ArgumentNullException(nameof(source6)),
            source7 ?? throw new ArgumentNullException(nameof(source7)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

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
        => new ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
            source1 ?? throw new ArgumentNullException(nameof(source1)),
            source2 ?? throw new ArgumentNullException(nameof(source2)),
            source3 ?? throw new ArgumentNullException(nameof(source3)),
            source4 ?? throw new ArgumentNullException(nameof(source4)),
            source5 ?? throw new ArgumentNullException(nameof(source5)),
            source6 ?? throw new ArgumentNullException(nameof(source6)),
            source7 ?? throw new ArgumentNullException(nameof(source7)),
            source8 ?? throw new ArgumentNullException(nameof(source8)),
            resultSelector ?? throw new ArgumentNullException(nameof(resultSelector)));

    private sealed class ZipIterator<T1, T2, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private readonly Func<T1, T2, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector) : base(2)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, TResult>(
                _p1.Source,
                _p2.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private Producer<T3> _p3;
        private readonly Func<T1, T2, T3, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector) : base(3)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            Producer<T3>.Init(out _p3, source3, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, T3, TResult>(
                _p1.Source,
                _p2.Source,
                _p3.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
            Pulse(ref _p3.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private Producer<T3> _p3;
        private Producer<T4> _p4;
        private readonly Func<T1, T2, T3, T4, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            Func<T1, T2, T3, T4, TResult> resultSelector) : base(4)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            Producer<T3>.Init(out _p3, source3, this);
            Producer<T4>.Init(out _p4, source4, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, T3, T4, TResult>(
                _p1.Source,
                _p2.Source,
                _p3.Source,
                _p4.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
            Pulse(ref _p3.TsIdle);
            Pulse(ref _p4.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private Producer<T3> _p3;
        private Producer<T4> _p4;
        private Producer<T5> _p5;
        private readonly Func<T1, T2, T3, T4, T5, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector) : base(5)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            Producer<T3>.Init(out _p3, source3, this);
            Producer<T4>.Init(out _p4, source4, this);
            Producer<T5>.Init(out _p5, source5, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, T3, T4, T5, TResult>(
                _p1.Source,
                _p2.Source,
                _p3.Source,
                _p4.Source,
                _p5.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
            Pulse(ref _p3.TsIdle);
            Pulse(ref _p4.TsIdle);
            Pulse(ref _p5.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private Producer<T3> _p3;
        private Producer<T4> _p4;
        private Producer<T5> _p5;
        private Producer<T6> _p6;
        private readonly Func<T1, T2, T3, T4, T5, T6, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            Func<T1, T2, T3, T4, T5, T6, TResult> resultSelector) : base(6)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            Producer<T3>.Init(out _p3, source3, this);
            Producer<T4>.Init(out _p4, source4, this);
            Producer<T5>.Init(out _p5, source5, this);
            Producer<T6>.Init(out _p6, source6, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, T3, T4, T5, T6, TResult>(
                _p1.Source,
                _p2.Source,
                _p3.Source,
                _p4.Source,
                _p5.Source,
                _p6.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
            Pulse(ref _p3.TsIdle);
            Pulse(ref _p4.TsIdle);
            Pulse(ref _p5.TsIdle);
            Pulse(ref _p6.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent(), _p6.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private Producer<T3> _p3;
        private Producer<T4> _p4;
        private Producer<T5> _p5;
        private Producer<T6> _p6;
        private Producer<T7> _p7;
        private readonly Func<T1, T2, T3, T4, T5, T6, T7, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> resultSelector) : base(7)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            Producer<T3>.Init(out _p3, source3, this);
            Producer<T4>.Init(out _p4, source4, this);
            Producer<T5>.Init(out _p5, source5, this);
            Producer<T6>.Init(out _p6, source6, this);
            Producer<T7>.Init(out _p7, source7, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult>(
                _p1.Source,
                _p2.Source,
                _p3.Source,
                _p4.Source,
                _p5.Source,
                _p6.Source,
                _p7.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
            Pulse(ref _p3.TsIdle);
            Pulse(ref _p4.TsIdle);
            Pulse(ref _p5.TsIdle);
            Pulse(ref _p6.TsIdle);
            Pulse(ref _p7.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent(), _p6.GetCurrent(), _p7.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : ZipIteratorBase<TResult>
    {
        private Producer<T1> _p1;
        private Producer<T2> _p2;
        private Producer<T3> _p3;
        private Producer<T4> _p4;
        private Producer<T5> _p5;
        private Producer<T6> _p6;
        private Producer<T7> _p7;
        private Producer<T8> _p8;
        private readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            IAsyncEnumerable<T6> source6,
            IAsyncEnumerable<T7> source7,
            IAsyncEnumerable<T8> source8,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> resultSelector) : base(8)
        {
            Producer<T1>.Init(out _p1, source1, this);
            Producer<T2>.Init(out _p2, source2, this);
            Producer<T3>.Init(out _p3, source3, this);
            Producer<T4>.Init(out _p4, source4, this);
            Producer<T5>.Init(out _p5, source5, this);
            Producer<T6>.Init(out _p6, source6, this);
            Producer<T7>.Init(out _p7, source7, this);
            Producer<T8>.Init(out _p8, source8, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
                _p1.Source,
                _p2.Source,
                _p3.Source,
                _p4.Source,
                _p5.Source,
                _p6.Source,
                _p7.Source,
                _p8.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            Pulse(ref _p1.TsIdle);
            Pulse(ref _p2.TsIdle);
            Pulse(ref _p3.TsIdle);
            Pulse(ref _p4.TsIdle);
            Pulse(ref _p5.TsIdle);
            Pulse(ref _p6.TsIdle);
            Pulse(ref _p7.TsIdle);
            Pulse(ref _p8.TsIdle);
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent(), _p6.GetCurrent(), _p7.GetCurrent(), _p8.GetCurrent());
    }

}
