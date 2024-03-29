﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Linx;
//using Linx.AsyncEnumerable;
//using Linx.Expressions;
//using Linx.Testing;
//using Linx.Timing;
//using Newtonsoft.Json.Linq;
//using Xunit;

//namespace Tests.Linx.AsyncEnumerable
//{
//    public class AggregatorTests
//    {
//        [Fact]
//        public async Task TestAggregate()
//        {
//            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().Aggregate(1, (a, c) => a * c, default);
//            Assert.Equal(6, result);

//            result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().Aggregate(1, (a, c) => a * c, a => -a, default);
//            Assert.Equal(-6, result);
//        }

//        [Fact]
//        public async Task TestAll()
//        {
//            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().All(x => x < 10, CancellationToken.None);
//            Assert.True(result);

//            result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().All(x => x < 2, CancellationToken.None);
//            Assert.False(result);

//            result = await LinxAsyncEnumerable.Empty<int>().All(x => x == 0, CancellationToken.None);
//            Assert.True(result);
//        }

//        [Fact]
//        public async Task TestAny()
//        {
//            var result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().Any(CancellationToken.None);
//            Assert.True(result);

//            result = await LinxAsyncEnumerable.Empty<int>().Any(CancellationToken.None);
//            Assert.False(result);

//            result = await new[] { 1, 2, 3 }.ToAsyncEnumerable().Any(x => x < 2, CancellationToken.None);
//            Assert.True(result);
//        }

//        [Fact]
//        public async Task TestAverage()
//        {
//            Assert.Equal(5.5, await Enumerable.Range(1, 10).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5, await Enumerable.Range(1, 10).Select(x => (long)x).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5f, await Enumerable.Range(1, 10).Select(x => (float)x).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5, await Enumerable.Range(1, 10).Select(x => (double)x).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5m, await Enumerable.Range(1, 10).Select(x => (decimal)x).ToAsyncEnumerable().Average(default));

//            Assert.Equal(5.5, await Enumerable.Range(1, 10).Select(x => new Int64Ratio(2 * x, 2)).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5f, await Enumerable.Range(1, 10).Select(x => new FloatRatio(2 * x, 2)).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5f, await Enumerable.Range(1, 10).Select(x => new DoubleRatio(2 * x, 2)).ToAsyncEnumerable().Average(default));
//            Assert.Equal(5.5m, await Enumerable.Range(1, 10).Select(x => new DecimalRatio(2 * x, 2)).ToAsyncEnumerable().Average(default));
//        }

//        [Fact]
//        public async Task TestContains()
//        {
//            Assert.True(await Enumerable.Range(1, 10).ToAsyncEnumerable().Contains(5, default));
//            Assert.False(await Enumerable.Range(1, 10).ToAsyncEnumerable().Contains(11, default));
//        }

//        [Fact]
//        public async Task TestCount()
//        {
//            Assert.Equal(10, await Enumerable.Range(1, 10).ToAsyncEnumerable().Count(default));
//            Assert.Equal(5, await Enumerable.Range(1, 10).ToAsyncEnumerable().Count(x => (x & 1) == 0, default));
//        }

//        [Fact]
//        public async Task TestElementAt()
//        {
//            Assert.Equal(5, await Enumerable.Range(1, 10).ToAsyncEnumerable().ElementAt(4, default));
//            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Enumerable.Range(1, 10).ToAsyncEnumerable().ElementAt(-1, default));

//            Assert.Equal(5, await Enumerable.Range(1, 10).ToAsyncEnumerable().ElementAtOrDefault(4, default));
//            Assert.Equal(0, await Enumerable.Range(1, 10).ToAsyncEnumerable().ElementAtOrDefault(-1, default));

//            Assert.Equal(5, await Enumerable.Range(1, 10).ToAsyncEnumerable().ElementAtOrNull(4, default));
//            Assert.Null(await Enumerable.Range(1, 10).ToAsyncEnumerable().ElementAtOrNull(-1, default));
//        }

//        [Fact]
//        public async Task TestFirst()
//        {
//            var src = new[] { 1, 2, 3 }.ToAsyncEnumerable();

//            // ReSharper disable PossibleMultipleEnumeration
//            Assert.Equal(1, await src.First(default));
//            Assert.Equal(2, await src.First(x => x > 1, default));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().First(CancellationToken.None));

//            Assert.Equal(1, await src.FirstOrDefault(default));
//            Assert.Equal(2, await src.FirstOrDefault(x => x > 1, default));
//            Assert.Equal(0, await LinxAsyncEnumerable.Empty<int>().FirstOrDefault(default));

//            Assert.Equal(1, await src.FirstOrNull(default));
//            Assert.Equal(2, await src.FirstOrNull(x => x > 1, default));
//            Assert.Null(await LinxAsyncEnumerable.Empty<int>().FirstOrNull(default));
//            // ReSharper restore PossibleMultipleEnumeration
//        }

//        [Fact]
//        public async Task TestLast()
//        {
//            var src = new[] { 1, 2, 3 }.ToAsyncEnumerable();

//            // ReSharper disable PossibleMultipleEnumeration
//            Assert.Equal(3, await src.Last(default));
//            Assert.Equal(2, await src.Last(x => x < 3, default));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Last(CancellationToken.None));

//            Assert.Equal(3, await src.LastOrDefault(default));
//            Assert.Equal(2, await src.LastOrDefault(x => x < 3, default));
//            Assert.Equal(0, await LinxAsyncEnumerable.Empty<int>().LastOrDefault(default));

//            Assert.Equal(3, await src.LastOrNull(default));
//            Assert.Equal(2, await src.LastOrNull(x => x < 3, default));
//            Assert.Null(await LinxAsyncEnumerable.Empty<int>().LastOrNull(default));
//            // ReSharper restore PossibleMultipleEnumeration
//        }

//        [Fact]
//        public async Task TestIgnoreElements()
//        {
//            await new[] { 1, 2, 3 }.ToAsyncEnumerable().IgnoreElements(default);
//        }

//        [Fact]
//        public async Task TestLongCount()
//        {
//            Assert.Equal(10L, await Enumerable.Range(1, 10).ToAsyncEnumerable().LongCount(default));
//            Assert.Equal(5L, await Enumerable.Range(1, 10).ToAsyncEnumerable().LongCount(x => (x & 1) == 0, default));
//        }

//        [Fact]
//        public async Task TestMax()
//        {
//            var src = new[] { 1, 2, 3 }.ToAsyncEnumerable();

//            // ReSharper disable PossibleMultipleEnumeration
//            Assert.Equal(3, await src.Max(default));
//            Assert.Equal(4, await src.Max(x => x + 1, default));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Max(default));

//            Assert.Equal(3, await src.MaxOrDefault(default));
//            Assert.Equal(4, await src.MaxOrDefault(x => x + 1, default));
//            Assert.Equal(0, await LinxAsyncEnumerable.Empty<int>().MaxOrDefault(default));

//            Assert.Equal(3, await src.MaxOrNull(default));
//            Assert.Equal(4, await src.MaxOrNull(x => x + 1, default));
//            Assert.Null(await LinxAsyncEnumerable.Empty<int>().MaxOrNull(default));
//            // ReSharper restore PossibleMultipleEnumeration
//        }

//        [Fact]
//        public async Task TestMaxBy()
//        {
//            var src = new[] { 1, 2, 3, 1, 2, 3 }.ToAsyncEnumerable();
//            Assert.True(new[] { 3, 3 }.SequenceEqual(await src.MaxBy(x => x + 1, default)));
//            Assert.False((await LinxAsyncEnumerable.Empty<int>().MaxBy(x => x + 1, default)).Any());
//        }

//        [Fact]
//        public async Task TestMin()
//        {
//            var src = new[] { 1, 2, 3 }.ToAsyncEnumerable();

//            // ReSharper disable PossibleMultipleEnumeration
//            Assert.Equal(1, await src.Min(default));
//            Assert.Equal(2, await src.Min(x => x + 1, default));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Min(default));

//            Assert.Equal(1, await src.MinOrDefault(default));
//            Assert.Equal(2, await src.MinOrDefault(x => x + 1, default));
//            Assert.Equal(0, await LinxAsyncEnumerable.Empty<int>().MinOrDefault(default));

//            Assert.Equal(1, await src.MinOrNull(default));
//            Assert.Equal(2, await src.MinOrNull(x => x + 1, default));
//            Assert.Null(await LinxAsyncEnumerable.Empty<int>().MinOrNull(default));
//            // ReSharper restore PossibleMultipleEnumeration
//        }

//        [Fact]
//        public async Task TestMinBy()
//        {
//            var src = new[] { 1, 2, 3, 1, 2, 3 }.ToAsyncEnumerable();
//            Assert.True(new[] { 1, 1 }.SequenceEqual(await src.MinBy(x => x + 1, default)));
//            Assert.False((await LinxAsyncEnumerable.Empty<int>().MinBy(x => x + 1, default)).Any());
//        }

//        [Fact]
//        public async Task TestMultiAggregate()
//        {
//            var result = await Enumerable.Range(1, 3).ToAsyncEnumerable().MultiAggregate(
//                (s, t) => s.Sum(t),
//                (s, t) => s.First(t),
//                (s, t) => s.ElementAt(1, t),
//                (sum, first, second) => new { sum, first, second },
//                default);
//            Assert.Equal(6, result.sum);
//            Assert.Equal(1, result.first);
//            Assert.Equal(2, result.second);
//        }

//        [Fact]
//        public async Task TestMultiAggregateFail()
//        {
//            var tResult = new[] { 2, 0, 1 }.ToAsyncEnumerable().MultiAggregate(
//                (s, t) => s.First(t),
//                (s, t) => s.ToList(t),
//                (s, t) => s.Select(i => 1 / i).Sum(t),
//                (first, all, sumInv) => new { first, all, sumInv },
//                default);
//            await Assert.ThrowsAsync<DivideByZeroException>(async () => await tResult);
//        }

//        [Fact]
//        public void TestMultiAggregateCancel()
//        {
//            VirtualTime.Run(async vt =>
//            {
//                var src = vt.Parse("01--2|").Select((c, i) => i);
//                var token = vt.CancelAt(vt.Now + vt.TimeFrame);

//                var testee = vt.Parse("01--2|")
//                    .Select((c, i) => i)
//                    .MultiAggregate(
//                        (s, t) => s.ToList(t),
//                        (s, t) => s.Sum(t),
//                        (all, sum) => (all, sum),
//                        token);

//                var oce = await Assert.ThrowsAsync<OperationCanceledException>(() => testee);
//            });

//            var run = Express.Func(async (VirtualTime vt) =>
//            {
//                var token = vt.CancelAt(vt.Now + vt.TimeFrame);
//                return await vt.Parse("01--2|")
//                    .Select((c, i) => i)
//                    .MultiAggregate(
//                        (s, t) => s.ToList(t),
//                        (s, t) => s.Sum(t),
//                        (all, sum) => (all, sum),
//                        token);
//            });

//            Assert.Throws<OperationCanceledException>(() => VirtualTime.Run(run));
//        }

//        [Fact]
//        public async Task TestMultiConsume()
//        {
//            static Task Good(IAsyncEnumerable<int> xs, CancellationToken t) => xs.IgnoreElements(t);
//            static Task Bad(IAsyncEnumerable<int> xs, CancellationToken t) => xs.Select((x, i) => i < 2 ? i.ToString() : throw new Exception("Boom!")).IgnoreElements(t);
//            await new[] { 1, 2, 3 }.ToAsyncEnumerable().MultiConsume(default, Good, Good);
//            await Assert.ThrowsAsync<Exception>(() => new[] { 1, 2, 3 }.ToAsyncEnumerable().MultiConsume(default, Good, Bad));
//        }

//        [Fact]
//        public async Task TestSingle()
//        {
//            var src = new[] { 1, 2, 3 }.ToAsyncEnumerable();

//            // ReSharper disable PossibleMultipleEnumeration
//            await Assert.ThrowsAsync<InvalidOperationException>(() => LinxAsyncEnumerable.Empty<int>().Single(CancellationToken.None));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => src.Single(CancellationToken.None));
//            Assert.Equal(2, await src.Single(x => x == 2, default));

//            Assert.Equal(0, await LinxAsyncEnumerable.Empty<int>().SingleOrDefault(default));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => src.SingleOrDefault(CancellationToken.None));
//            Assert.Equal(2, await src.SingleOrDefault(x => x == 2, default));

//            Assert.Null(await LinxAsyncEnumerable.Empty<int>().SingleOrNull(default));
//            await Assert.ThrowsAsync<InvalidOperationException>(() => src.SingleOrNull(CancellationToken.None));
//            Assert.Equal(2, await src.SingleOrNull(x => x == 2, default));
//            // ReSharper restore PossibleMultipleEnumeration
//        }

//        [Fact]
//        public async Task TestToList()
//        {
//            var source = new[] { 1, 2, 3 }.ToAsyncEnumerable();
//            var result = await source.ToList(CancellationToken.None);
//            Assert.True(new[] { 1, 2, 3 }.SequenceEqual(result));
//        }
//    }
//}
