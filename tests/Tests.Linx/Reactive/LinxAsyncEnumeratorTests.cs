//namespace Tests.Linx.Reactive
//{
//    using System;
//    using System.Threading;
//    using System.Threading.Tasks;
//    using global::Linx.Reactive;
//    using Xunit;

//    public sealed class LinxAsyncEnumeratorTests
//    {
//        [Fact]
//        public async Task Canceled()
//        {
//            CancellationToken canceledToken;
//            {
//                var cts = new CancellationTokenSource();
//                cts.Cancel();
//                canceledToken = cts.Token;
//            }

//            var calledBack = false;
//            var ae = new LinxAsyncEnumerator<int>((onNextAsync, t) => { calledBack = true; return Task.CompletedTask; }, canceledToken);
//            try { await ae.MoveNextAsync().ConfigureAwait(false); }
//            catch (OperationCanceledException oce) when (oce.CancellationToken == canceledToken) { }
//            finally { await ae.DisposeAsync().ConfigureAwait(false); }

//            if (calledBack) throw new Exception("Should not be called back.");
//        }

//        [Fact]
//        public async Task Complete()
//        {
//            var ae = new LinxAsyncEnumerator<int>(async (onNextAsync, t) =>
//            {
//                await onNextAsync(1).ConfigureAwait(false);
//                await onNextAsync(2).ConfigureAwait(false);
//                await onNextAsync(3).ConfigureAwait(false);
//            }, CancellationToken.None);
//            try
//            {
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 1);
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 2);
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 3);
//                Assert.False(await ae.MoveNextAsync().ConfigureAwait(false));
//            }
//            finally { await ae.DisposeAsync().ConfigureAwait(false); }
//            Assert.Equal(default, ae.Current);
//        }

//        [Fact]
//        public async Task Dispose()
//        {
//            var wasCanceled = false;
//            var ae = new LinxAsyncEnumerator<int>(async (onNextAsync, t) =>
//            {
//                await onNextAsync(1).ConfigureAwait(false);
//                await onNextAsync(2).ConfigureAwait(false);
//                try { await onNextAsync(3).ConfigureAwait(false); }
//                catch (OperationCanceledException oce) when (oce.CancellationToken == t) { wasCanceled = true; throw; }
//            }, CancellationToken.None);

//            try
//            {
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 1);
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 2);
//            }
//            finally { await ae.DisposeAsync().ConfigureAwait(false); }

//            Assert.Equal(default, ae.Current);
//            if (!wasCanceled) throw new Exception("Should have canceled.");
//        }

//        [Fact]
//        public async Task Cancel()
//        {
//            var cts = new CancellationTokenSource();

//            var wasCanceled = false;
//            var ae = new LinxAsyncEnumerator<int>(async (onNextAsync, t) =>
//            {
//                await onNextAsync(1).ConfigureAwait(false);
//                await onNextAsync(2).ConfigureAwait(false);
//                try { await onNextAsync(3).ConfigureAwait(false); }
//                catch (OperationCanceledException oce) when (oce.CancellationToken == t) { wasCanceled = true; throw; }
//            }, cts.Token);

//            try
//            {
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 1);
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 2);
//                cts.Cancel();
//                try
//                {
//                    await ae.MoveNextAsync().ConfigureAwait(false);
//                    throw new Exception("Should throw OCE.");
//                }
//                catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token) { }
//            }
//            finally { await ae.DisposeAsync().ConfigureAwait(false); }

//            Assert.Equal(default, ae.Current);
//            if (!wasCanceled) throw new Exception("Should have canceled.");
//        }

//        [Fact]
//        public async Task Throw()
//        {
//            var ae = new LinxAsyncEnumerator<int>(async (onNextAsync, t) =>
//            {
//                await onNextAsync(1).ConfigureAwait(false);
//                await onNextAsync(2).ConfigureAwait(false);
//                throw new Exception("Boom!");
//            }, CancellationToken.None);
//            try
//            {
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 1);
//                Assert.True(await ae.MoveNextAsync().ConfigureAwait(false) && ae.Current == 2);
//                await ae.MoveNextAsync().ConfigureAwait(false);
//                throw new Exception("Should have failed.");
//            }
//            catch (Exception ex) when (ex.Message == "Boom!") { }
//            finally { await ae.DisposeAsync().ConfigureAwait(false); }
//            Assert.Equal(default, ae.Current);
//        }
//    }
//}
