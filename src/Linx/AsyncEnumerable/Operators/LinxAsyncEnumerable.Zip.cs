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
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Func<T1, T2, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            Func<T1, T2, TResult> resultSelector) : base(2)
        {
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _resultSelector = resultSelector;
        }

        protected override ZipIteratorBase<TResult> Clone() =>
            new ZipIterator<T1, T2, TResult>(
                _p1.Source,
                _p2.Source,
                _resultSelector);

        protected override void PulseAll()
        {
            _p1.Pulse();
            _p2.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, TResult> : ZipIteratorBase<TResult>
    {
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Producer<T3> _p3;
        private readonly Func<T1, T2, T3, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            Func<T1, T2, T3, TResult> resultSelector) : base(3)
        {
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _p3 = new Producer<T3>(source3, this);
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
            _p1.Pulse();
            _p2.Pulse();
            _p3.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, TResult> : ZipIteratorBase<TResult>
    {
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Producer<T3> _p3;
        private readonly Producer<T4> _p4;
        private readonly Func<T1, T2, T3, T4, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            Func<T1, T2, T3, T4, TResult> resultSelector) : base(4)
        {
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _p3 = new Producer<T3>(source3, this);
            _p4 = new Producer<T4>(source4, this);
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
            _p1.Pulse();
            _p2.Pulse();
            _p3.Pulse();
            _p4.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, TResult> : ZipIteratorBase<TResult>
    {
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Producer<T3> _p3;
        private readonly Producer<T4> _p4;
        private readonly Producer<T5> _p5;
        private readonly Func<T1, T2, T3, T4, T5, TResult> _resultSelector;

        public ZipIterator(
            IAsyncEnumerable<T1> source1,
            IAsyncEnumerable<T2> source2,
            IAsyncEnumerable<T3> source3,
            IAsyncEnumerable<T4> source4,
            IAsyncEnumerable<T5> source5,
            Func<T1, T2, T3, T4, T5, TResult> resultSelector) : base(5)
        {
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _p3 = new Producer<T3>(source3, this);
            _p4 = new Producer<T4>(source4, this);
            _p5 = new Producer<T5>(source5, this);
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
            _p1.Pulse();
            _p2.Pulse();
            _p3.Pulse();
            _p4.Pulse();
            _p5.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, TResult> : ZipIteratorBase<TResult>
    {
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Producer<T3> _p3;
        private readonly Producer<T4> _p4;
        private readonly Producer<T5> _p5;
        private readonly Producer<T6> _p6;
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
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _p3 = new Producer<T3>(source3, this);
            _p4 = new Producer<T4>(source4, this);
            _p5 = new Producer<T5>(source5, this);
            _p6 = new Producer<T6>(source6, this);
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
            _p1.Pulse();
            _p2.Pulse();
            _p3.Pulse();
            _p4.Pulse();
            _p5.Pulse();
            _p6.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent(), _p6.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, T7, TResult> : ZipIteratorBase<TResult>
    {
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Producer<T3> _p3;
        private readonly Producer<T4> _p4;
        private readonly Producer<T5> _p5;
        private readonly Producer<T6> _p6;
        private readonly Producer<T7> _p7;
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
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _p3 = new Producer<T3>(source3, this);
            _p4 = new Producer<T4>(source4, this);
            _p5 = new Producer<T5>(source5, this);
            _p6 = new Producer<T6>(source6, this);
            _p7 = new Producer<T7>(source7, this);
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
            _p1.Pulse();
            _p2.Pulse();
            _p3.Pulse();
            _p4.Pulse();
            _p5.Pulse();
            _p6.Pulse();
            _p7.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent(), _p6.GetCurrent(), _p7.GetCurrent());
    }

    private sealed class ZipIterator<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : ZipIteratorBase<TResult>
    {
        private readonly Producer<T1> _p1;
        private readonly Producer<T2> _p2;
        private readonly Producer<T3> _p3;
        private readonly Producer<T4> _p4;
        private readonly Producer<T5> _p5;
        private readonly Producer<T6> _p6;
        private readonly Producer<T7> _p7;
        private readonly Producer<T8> _p8;
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
            _p1 = new Producer<T1>(source1, this);
            _p2 = new Producer<T2>(source2, this);
            _p3 = new Producer<T3>(source3, this);
            _p4 = new Producer<T4>(source4, this);
            _p5 = new Producer<T5>(source5, this);
            _p6 = new Producer<T6>(source6, this);
            _p7 = new Producer<T7>(source7, this);
            _p8 = new Producer<T8>(source8, this);
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
            _p1.Pulse();
            _p2.Pulse();
            _p3.Pulse();
            _p4.Pulse();
            _p5.Pulse();
            _p6.Pulse();
            _p7.Pulse();
            _p8.Pulse();
        }

        protected override TResult GetCurrent() => _resultSelector(_p1.GetCurrent(), _p2.GetCurrent(), _p3.GetCurrent(), _p4.GetCurrent(), _p5.GetCurrent(), _p6.GetCurrent(), _p7.GetCurrent(), _p8.GetCurrent());
    }

}
