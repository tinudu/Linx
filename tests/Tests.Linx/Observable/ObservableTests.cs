namespace Tests.Linx.Observable
{
    using System;
    using System.Threading;
    using global::Linx.AsyncEnumerable.Testing;
    using global::Linx.Observable;
    using global::Linx.Timing;
    using Xunit;

    public sealed class ObservableTests
    {
        [Fact]
        public async void TestAsyncSuccess()
        {
            using (var vt = new VirtualTime())
            {
                var src = new TestObservable(MarbleParserSettings.DefaultFrameSize, 4);
                var testee = src.Async();
                var expect = Marble.Parse("-0-1-2-3|", (c, i) => i);
                var eq = testee.AssertEqual(expect);
                vt.Start();
                await eq;
            }
        }


        private sealed class CancellationDisposable : IDisposable
        {
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            public CancellationToken Token => _cts.Token;
            public void Dispose() => _cts.Cancel();
        }

        private sealed class TestObservable : IObservable<int>
        {
            private readonly TimeSpan _interval;
            private readonly int _take;

            public TestObservable(TimeSpan interval, int take)
            {
                _interval = interval;
                _take = take;
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                var disposable = new CancellationDisposable();
                Generate(observer, disposable.Token);
                return disposable;
            }

            private async void Generate(IObserver<int> observer, CancellationToken token)
            {
                try
                {
                    using (var timer = Time.Current.GetTimer(token))
                        for (var i = 0; i < _take; i++)
                        {
                            await timer.Delay(_interval).ConfigureAwait(false);
                            observer.OnNext(i);
                        }
                    observer.OnCompleted();
                }
                catch (Exception ex) { observer.OnError(ex); }
            }
        }
    }
}
