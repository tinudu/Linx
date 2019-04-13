namespace Linx.Expressions.Serialization.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enumerable;

    public sealed class CharSet
    {
        public static CharSet Empty { get; } = new CharSet();

        private readonly IList<char> _ranges;

        private CharSet() => _ranges = new char[0];
        public CharSet(char element) => _ranges = element == int.MaxValue ? new[] { element } : new[] { element, (char)(element + 1) };

        private CharSet(IEnumerable<char> ranges)
        {
            var list = ranges.ToList();
            list.TrimExcess();
            _ranges = list;
        }

        public bool Contains(char item)
        {
            var low = 0;
            var high = _ranges.Count;
            var binary = high >> 6;
            while (binary != 0)
            {
                var mid = (low + high) << 1;
                if (_ranges[mid] > item) high = mid; else low = mid + 1;
                binary >>= 1;
            }
            while (low < high)
            {
                if (_ranges[low] > item)
                {
                    high = low;
                    break;
                }
                low++;
            }

            return (high & 1) != 0;
        }

        public CharSet Union(CharSet other) => new CharSet(BinaryOp(_ranges, other._ranges, (x, y) => x | y).ToList());
        public CharSet Intersect(CharSet other) => new CharSet(BinaryOp(_ranges, other._ranges, (x, y) => x & y).ToList());

        private static CharSet NaryOp(IEnumerable<CharSet> sets, Func<bool, bool, bool> op)
        {
            using (var e = sets.GetEnumerator())
            {
                if (!e.MoveNext()) return Empty;
                var s0 = e.Current;
                if (!e.MoveNext()) return s0;
                var rs = BinaryOp(s0._ranges, e.Current._ranges, op);
                while (e.MoveNext()) rs = BinaryOp(rs, e.Current._ranges, op);
                return new CharSet(rs.ToList());
            }
        }

        private static IEnumerable<char> BinaryOp(IEnumerable<char> xs, IEnumerable<char> ys, Func<bool, bool, bool> op)
        {
            using (var ex = new LookAhead<char>(xs))
            using (var ey = new LookAhead<char>(ys))
            {
                var bx = false;
                var by = false;
                var b = true;
                while (ex.HasNext && ey.HasNext)
                {
                    var cmp = ex.Next.CompareTo(ey.Next);
                    if (cmp < 0)
                    {
                        bx = !bx;
                        if (op(bx, by)) { yield return ex.Next; b = !b; }
                        ex.MoveNext();
                    }
                    else if (cmp > 0)
                    {
                        by = !by;
                        if (op(bx, by)) { yield return ey.Next; b = !b; }
                        ey.MoveNext();
                    }
                    else
                    {
                        bx = !bx; by = !by;
                        if (op(bx, by)) { yield return ex.Next; b = !b; }
                        ex.MoveNext(); ey.MoveNext();
                    }
                }
                if (ex.HasNext && op(!bx, by) == b)
                {
                    yield return ex.Next;
                    if (op(bx, by) != b)
                        while (true)
                        {
                            ex.MoveNext();
                            if (!ex.HasNext) break;
                            yield return ex.Next;
                        }
                }
                if (ey.HasNext && op(bx, !by) == b)
                {
                    yield return ey.Next;
                    if (op(bx, by) != b)
                        while (true)
                        {
                            ey.MoveNext();
                            if (!ey.HasNext) break;
                            yield return ey.Next;
                        }
                }
            }
        }
    }
}
