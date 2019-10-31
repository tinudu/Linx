namespace Tests.Linx.AsyncEnumerable
{
    using global::Linx.AsyncEnumerable;
    using global::Linx.Testing;
    using global::Linx.Timing;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    internal static class MyOperators
    {
        public static IAsyncEnumerable<T> ConsumeSlow<T>(this IAsyncEnumerable<T> source, TimeSpan delay) => LinxAsyncEnumerable.Create<T>(async (yield, token) =>
        {
            var ae = source.WithCancellation(token).ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                using var timer = Time.Current.GetTimer(token);
                while (await ae.MoveNextAsync())
                {
                    if (!await yield(ae.Current).ConfigureAwait(false)) return;
                    await timer.Delay(delay).ConfigureAwait(false);
                }
            }
            finally { await ae.DisposeAsync(); }
        });
    }

    public sealed class OperatorTests
    {
        [Fact]
        public async Task TestConcat()
        {
            var r = Enumerable.Range(0, 3).Async();
            // ReSharper disable PossibleMultipleEnumeration
            var result = await r.Concat(r, r).ToList(default);
            // ReSharper restore PossibleMultipleEnumeration
            Assert.True(new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 }.SequenceEqual(result));
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
            Assert.Equal(2, result.Count);
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
        public async Task TestZip()
        {
            var src1 = Marble.Parse("a-  - -bc- -d- -e");
            var src2 = Marble.Parse(" -fg-h-  -i- -|  ");
            var expt = Marble.Parse(" -x - -xx- -x-|", default, "af", "bg", "ch", "di");
            var testee = src1.Zip(src2, (x, y) => $"{x}{y}");
            using var vt = new VirtualTime();
            var eq = expt.AssertEqual(testee, default);
            vt.Start();
            await eq;
        }
    }
}