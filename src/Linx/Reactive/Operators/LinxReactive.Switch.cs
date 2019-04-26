namespace Linx.Reactive
{
    using System;
    using System.Threading;

    partial class LinxReactive
    {
        /// <summary>
        /// Transforms a sequence of sequences into an sequence producing values only from the most recent sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Switch<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Produce<T>(async (yield, token) =>
            {
                CancellationTokenSource ctsInner = null;

                void CancelInner()
                {
                    // ReSharper disable once AccessToModifiedClosure
                    var cts = Interlocked.Exchange(ref ctsInner, null);
                    if (cts == null) return;
                    try { cts.Cancel(); } catch { /**/ }
                }

                var aeOuter = sources
                    .Do(v => CancelInner(), e => CancelInner())
                    .Latest()
                    .GetAsyncEnumerator(token);
                try
                {
                    while (await aeOuter.MoveNextAsync())
                    {
                        var cts = new CancellationTokenSource();
                        ctsInner = cts;
                        try
                        {
                            var aeInner = aeOuter.Current.GetAsyncEnumerator(cts.Token);
                            try
                            {
                                while (await aeInner.MoveNextAsync())
                                    await yield(aeInner.Current);
                            }
                            finally { await aeInner.DisposeAsync().ConfigureAwait(false); }
                        }
                        catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token) { }
                        finally { CancelInner(); }
                    }
                }
                finally { await aeOuter.DisposeAsync().ConfigureAwait(false); }
            });
        }
    }
}
