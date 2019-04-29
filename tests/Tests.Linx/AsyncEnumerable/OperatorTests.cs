namespace Tests.Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.AsyncEnumerable;
    using global::Linx.AsyncEnumerable.Timing;
    using Xunit;

    internal static class MyOperators
    {
        public static IAsyncEnumerable<T> ObserveAfter<T>(this IAsyncEnumerable<T> source, TimeSpan delay) => LinxAsyncEnumerable.Produce<T>(async (yield, token) =>
        {
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var time = Time.Current;
                while (await ae.MoveNextAsync())
                {
                    Debug.WriteLine($"Next@{Time.Current.Now.TimeOfDay.TotalSeconds}");
                    await time.Delay(delay, token).ConfigureAwait(false);
                    Debug.WriteLine($"Observing {ae.Current}@{Time.Current.Now.TimeOfDay.TotalSeconds}");
                    await yield(ae.Current);
                }
            }
            finally { await ae.DisposeAsync(); }
        });

        public static IAsyncEnumerable<T> ObserveImmediate<T>(this IAsyncEnumerable<T> source, TimeSpan delay) => LinxAsyncEnumerable.Produce<T>(async (yield, token) =>
        {
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                var time = Time.Current;
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    await time.Delay(delay, token).ConfigureAwait(false);
                    Assert.Equal(current, ae.Current);
                    await yield(current);
                }
            }
            finally { await ae.DisposeAsync(); }
        });
    }

    public sealed class OperatorTests
    {
        [Fact]
        public async Task TestCombineLatest()
        {
            using (new VirtualTime())
            {
                var seq1 = LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(0.5)).Skip(1).Take(8);
                var seq2 = LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(0.7)).Skip(1).Take(7);
                var testee = seq1.CombineLatest(seq2, (x, y) => 10 * x + y);
                // seq1: ....1....2....3....4....5....6....7....8
                // seq2: ......1......2......3......4......5......6......7
                var result = await testee.ToList(default);
                Assert.True(new[] { 11L, 21, 22, 32, 42, 43, 53, 54, 64, 65, 75, 85, 86, 87 }.SequenceEqual(result));
            }
        }


        [Fact]
        public async Task TestConcat()
        {
            var r = LinxAsyncEnumerable.Range(0, 3);
            var result = await r.Concat(r).Concat(r).ToList(default);
            Assert.True(new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 }.SequenceEqual(result));
        }

        [Fact]
        public async Task TestDelay()
        {
            using (new VirtualTime())
            {
                var source = Enumerable.Range(0, 10).Async().Zip(LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(0.5)), (i, t) => i);
                var tResult = source.Delay(TimeSpan.FromSeconds(3)).ToList(default);
                var result = await tResult;
                Assert.True(Enumerable.Range(0, 10).SequenceEqual(result));
            }
        }

        [Fact]
        public async Task TestLatest()
        {
            using (new VirtualTime())
            {
                var tResult = LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(1)).Take(15).Select(i => (int)i).Latest()
                    .ObserveAfter(TimeSpan.FromSeconds(3.7)).ToList(default);
                var result = await tResult;
                Assert.True(new[] { 3, 7, 11, 14 }.SequenceEqual(result));
            }

            using (new VirtualTime())
            {
                var tResult = LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(1)).Take(15).Select(i => (int)i).Latest()
                    .ObserveImmediate(TimeSpan.FromSeconds(3.7)).ToList(default);
                var result = await tResult;
                Assert.True(new[] { 0, 3, 7, 11, 14 }.SequenceEqual(result));
            }
        }

        [Fact]
        public async Task TestGroupBy()
        {
            var result = await new[] { 1, 2, 1, 3, 2, 3, 1, 1, 2 }.Async().GroupBy(i => i).Parallel(async (g, t) => new { g.Key, Count = await g.Count(t) }).ToDictionary(kc => kc.Key, kc => kc.Count, default);
            Assert.Equal(4, result[1]);
            Assert.Equal(3, result[2]);
            Assert.Equal(2, result[3]);

            result = await new[] { 1, 2, 1, 3, 2, 3, 1, 1, 2 }.Async().GroupBy(i => i).Parallel(async (g, t) => new { g.Key, Count = await g.Take(3).Count(t) }).ToDictionary(kc => kc.Key, kc => kc.Count, default);
            Assert.Equal(3, result[1]);
            Assert.Equal(3, result[2]);
            Assert.Equal(2, result[3]);

            result = await new[] { 1, 2, 1, 3, 2, 3, 1, 1, 2 }.Async().GroupBy(i => i).Take(2).Parallel(async (g, t) => new { g.Key, Count = await g.Take(2).Count(t) }).ToDictionary(kc => kc.Key, kc => kc.Count, default);
            Assert.Equal(2, result[1]);
            Assert.Equal(2, result[2]);
        }

        [Fact]
        public async Task TestParallel()
        {
            {
                var ctss = Enumerable.Range(0, 5).Select(i => new TaskCompletionSource<int>()).ToList();
                var tResult = ctss.Async().Parallel((cts, t) => cts.Task).ToList(CancellationToken.None);
                foreach (var i in new[] { 3, 1, 4, 0, 2 })
                {
                    ctss[i].TrySetResult(i);
                    await Task.Delay(1);
                }
                var result = await tResult;
                Assert.True(new[] { 3, 1, 4, 0, 2 }.SequenceEqual(result));
            }

            {
                var ctss = Enumerable.Range(0, 5).Select(i => new TaskCompletionSource<int>()).ToList();
                var tResult = ctss.Async().Parallel((cts, t) => cts.Task, true).ToList(CancellationToken.None);
                foreach (var i in new[] { 3, 1, 4, 0, 2 }) ctss[i].TrySetResult(i);
                var result = await tResult;
                Assert.True(new[] { 0, 1, 2, 3, 4 }.SequenceEqual(result));
            }

            {
                var ctss = Enumerable.Range(0, 5).Select(i => new TaskCompletionSource<int>()).ToList();
                var tResult = ctss.Async().Parallel((cts, t) => cts.Task, false, 2).ToList(CancellationToken.None);
                foreach (var i in new[] { 3, 1, 4, 0, 2 })
                {
                    ctss[i].TrySetResult(i);
                    await Task.Delay(1);
                }
                var result = await tResult;
                Assert.True(new[] { 1, 0, 3, 4, 2 }.SequenceEqual(result));
            }

            {
                var ctss = Enumerable.Range(0, 5).Select(i => new TaskCompletionSource<int>()).ToList();
                var tResult = ctss.Async().Parallel((cts, t) => cts.Task, true, 2).ToList(CancellationToken.None);
                foreach (var i in new[] { 3, 1, 4, 0, 2 }) ctss[i].TrySetResult(i);
                var result = await tResult;
                Assert.True(new[] { 0, 1, 2, 3, 4 }.SequenceEqual(result));
            }
        }

        [Fact]
        public async Task TestTake()
        {
            var result = await new[] { 1, 2, 3, 4 }.Async().Where(i => i != 2).Take(2).ToList(CancellationToken.None);
            Assert.True(new[] { 1, 3 }.SequenceEqual(result));
        }

        [Fact]
        public async Task TestTimeout()
        {
            var source = LinxAsyncEnumerable.Produce<int>(async (yield, token) =>
            {
                var time = Time.Current;
                foreach (var i in Enumerable.Range(1, 10))
                {
                    await time.Delay(TimeSpan.FromSeconds(i), token).ConfigureAwait(false);
                    await yield(i);
                }
            });

            using (new VirtualTime())
            {
                var testee = source.Timeout(TimeSpan.FromSeconds(3.5));
                var ae = testee.ConfigureAwait(false).GetAsyncEnumerator();
                try
                {
                    Assert.True(await ae.MoveNextAsync() && ae.Current == 1);
                    Assert.True(await ae.MoveNextAsync() && ae.Current == 2);
                    Assert.True(await ae.MoveNextAsync() && ae.Current == 3);
                    await Assert.ThrowsAsync<TimeoutException>(async () => await ae.MoveNextAsync());
                }
                finally { await ae.DisposeAsync(); }
            }
        }

        [Fact]
        public async Task TestZip()
        {
            using (new VirtualTime())
            {
                var src1 = LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(1));
                var src2 = LinxAsyncEnumerable.Interval(TimeSpan.FromSeconds(2)).Take(4);
                var tResult = src1.Zip(src2, (x, y) => x + y).ToList(default);
                var result = await tResult;
                Assert.True(new[] { 0L, 2L, 4L, 6L }.SequenceEqual(result));
            }
        }
    }
}