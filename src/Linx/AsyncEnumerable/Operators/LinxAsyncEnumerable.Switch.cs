namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Transforms a sequence of sequences into an sequence producing values only from the most recent sequence.
        /// </summary>
        public static IAsyncEnumerable<T> Switch<T>(this IAsyncEnumerable<IAsyncEnumerable<T>> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));

            return Create<T>(async (yield, token) =>
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
                    .WithCancellation(token)
                    .ConfigureAwait(false)
                    .GetAsyncEnumerator();
                try
                {
                    while (await aeOuter.MoveNextAsync())
                    {
                        var cts = new CancellationTokenSource();
                        ctsInner = cts;
                        try
                        {
                            var aeInner = aeOuter.Current.WithCancellation(cts.Token).ConfigureAwait(false).GetAsyncEnumerator();
                            try
                            {
                                while (await aeInner.MoveNextAsync())
                                    if (!await yield(aeInner.Current).ConfigureAwait(false))
                                        return;
                            }
                            finally { await aeInner.DisposeAsync(); }
                        }
                        catch (OperationCanceledException oce) when (oce.CancellationToken == cts.Token) { }
                        finally { CancelInner(); }
                    }
                }
                finally { await aeOuter.DisposeAsync(); }
            });
        }
    }
}
