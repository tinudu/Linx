using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Linx
{
    /// <summary>
    /// Encapsulates a <see cref="CancellationTokenSource"/>.
    /// </summary>
    public class LinxCancellationTokenSource
    {
        private CancellationTokenSource _cts = new();
        private AsyncTaskMethodBuilder<OperationCanceledException> _atmb;

        /// <summary>
        /// Initialize.
        /// </summary>
        public LinxCancellationTokenSource()
        {
            Token = _cts.Token;
            WhenCancellationRequested = _atmb.Task;
        }

        /// <summary>
        /// Gets the associated <see cref="CancellationToken"/>.
        /// </summary>
        public CancellationToken Token { get; init; }

        /// <summary>
        /// Gets a <see cref="Task{TResult}"/> that completes when cancellation was requested.
        /// </summary>
        public Task<OperationCanceledException> WhenCancellationRequested { get; init; }

        /// <summary>
        /// Defensive cancelling.
        /// </summary>
        public void TryCancel()
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            if (cts is null) return;
            var atmb = Linx.Clear(ref _atmb);

            try { cts.Cancel(); }
            catch { /**/ }

            try { atmb.SetResult(new OperationCanceledException(Token)); }
            catch (Exception ex) { atmb.SetException(ex); }
        }
    }
}
