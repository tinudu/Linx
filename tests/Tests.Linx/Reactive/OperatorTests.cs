namespace Tests.Linx.Reactive
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Linx.Reactive;
    using global::Linx.Reactive.Timing;
    using Xunit;

    internal static class MyOperators
    {
        public static IAsyncEnumerable<T> ObserveAfter<T>(this IAsyncEnumerable<T> source, TimeSpan delay) => LinxReactive.Produce<T>(async (yield, token) =>
        {
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var time = Time.Current;
                while (await ae.MoveNextAsync())
                {
                    Debug.WriteLine($"Next@{Time.Current.Now.TimeOfDay.TotalSeconds}");
                    await time.Wait(delay, token).ConfigureAwait(false);
                    Debug.WriteLine($"Observing {ae.Current}@{Time.Current.Now.TimeOfDay.TotalSeconds}");
                    await yield(ae.Current);
                }
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        });

        public static IAsyncEnumerable<T> ObserveImmediate<T>(this IAsyncEnumerable<T> source, TimeSpan delay) => LinxReactive.Produce<T>(async (yield, token) =>
        {
            var ae = source.GetAsyncEnumerator(token);
            try
            {
                var time = Time.Current;
                while (await ae.MoveNextAsync())
                {
                    var current = ae.Current;
                    await time.Wait(delay, token).ConfigureAwait(false);
                    Assert.Equal(current, ae.Current);
                    await yield(current);
                }
            }
            finally { await ae.DisposeAsync().ConfigureAwait(false); }
        });
    }

    public sealed class OperatorTests
    {
        [Fact]
        public async Task TestCombineLatest()
        {
            using (var vt = new VirtualTime())
            {
                var seq1 = LinxReactive.Interval(TimeSpan.FromSeconds(0.5)).Skip(1).Take(8);
                var seq2 = LinxReactive.Interval(TimeSpan.FromSeconds(0.7)).Skip(1).Take(7);
                var testee = seq1.CombineLatest(seq2, (x, y) => 10 * x + y);
                // seq1: ....1....2....3....4....5....6....7....8
                // seq2: ......1......2......3......4......5......6......7
                var tResult = testee.ToList(default);
                vt.Start();
                var result = await tResult;
                Assert.True(new[] { 11L, 21, 22, 32, 42, 43, 53, 54, 64, 65, 75, 85, 86, 87 }.SequenceEqual(result));
            }
        }


        [Fact]
        public async Task TestConcat()
        {
            var r = LinxReactive.Range(0, 3);
            var result = await r.Concat(r).Concat(r).ToList(default);
            Assert.True(new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 }.SequenceEqual(result));
        }

        [Fact]
        public async Task TestDelay()
        {
            using (var vt = new VirtualTime())
            {
                var source = Enumerable.Range(0, 10).Async().Zip(LinxReactive.Interval(TimeSpan.FromSeconds(0.5)), (i, t) => i);
                var tResult = source.Delay(TimeSpan.FromSeconds(3)).ToList(default);
                vt.Start();
                var result = await tResult;
                Assert.True(Enumerable.Range(0, 10).SequenceEqual(result));
            }
        }

        [Fact]
        public async Task TestLatest()
        {
            using (var vt = new VirtualTime())
            {
                var tResult = LinxReactive.Interval(TimeSpan.FromSeconds(1))
                    .Take(15)
                    .Select(i => (int)i)
                    .Latest()
                    .ObserveAfter(TimeSpan.FromSeconds(3.7))
                    .ToList(default);
                vt.Start();
                var result = await tResult;
                Assert.True(new[] { 3, 7, 11, 14 }.SequenceEqual(result));
            }

            using (var vt = new VirtualTime())
            {
                var tResult = LinxReactive.Interval(TimeSpan.FromSeconds(1))
                    .Take(15)
                    .Select(i => (int)i)
                    .Latest()
                    .ObserveImmediate(TimeSpan.FromSeconds(3.7))
                    .ToList(default);
                vt.Start();
                var result = await tResult;
                Assert.True(new[] { 0, 3, 7, 11, 14 }.SequenceEqual(result));
            }
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
        public async Task TestZip()
        {
            using (var vt = new VirtualTime())
            {
                var src1 = LinxReactive.Interval(TimeSpan.FromSeconds(1));
                var src2 = LinxReactive.Interval(TimeSpan.FromSeconds(2)).Take(4);
                var tResult = src1.Zip(src2, (x, y) => x + y).ToList(default);
                vt.Start();
                var result = await tResult;
                Assert.True(new[] { 0L, 2L, 4L, 6L }.SequenceEqual(result));
            }
        }
    }
}