namespace Linx.AsyncEnumerable.Subjects
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Tasks;

    internal sealed class Enumerator<T> : IAsyncEnumerator<T>
    {
        private readonly ISubject<T> _subject;
        private CancellationTokenRegistration _ctr;

        public ManualResetValueTaskSource<bool> TsAccepting = new ManualResetValueTaskSource<bool>();
        public ManualResetValueTaskSource<bool> TsEmitting = new ManualResetValueTaskSource<bool>();
        public EnumeratorState State;
        public Exception Error;
        public Task Disposed;

        public Enumerator(ISubject<T> subject, CancellationToken token)
        {
            _subject = subject;
            if (token.CanBeCanceled) _ctr = token.Register(() => SetFinal(new OperationCanceledException(token)));
        }

        public T Current { get; set; }

        ValueTask<bool> IAsyncEnumerator<T>.MoveNextAsync()
        {
            TsAccepting.Reset();

            _subject.Gate.Wait();
            switch (State)
            {
                case EnumeratorState.Initial:
                    State = EnumeratorState.Accepting;
                    _subject.AddLocked(this);
                    break;

                case EnumeratorState.Emitting:
                    State = EnumeratorState.Accepting;
                    _subject.Gate.Set();
                    TsEmitting.SetResult(true);
                    break;

                case EnumeratorState.Final:
                    Current = default;
                    _subject.Gate.Set();
                    TsAccepting.SetExceptionOrResult(Error, false);
                    break;

                case EnumeratorState.Accepting:
                    throw new Exception(State + "???");

                default:
                    throw new Exception(State + "???");
            }

            return TsAccepting.Task;
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            SetFinal(AsyncEnumeratorDisposedException.Instance);
            return new ValueTask(Disposed);
        }

        public void SetFinal(Exception errorOpt)
        {
            _subject.Gate.Wait();
            switch (State)
            {
                case EnumeratorState.Initial:
                    Error = errorOpt;
                    Disposed = Task.CompletedTask;
                    State = EnumeratorState.Final;
                    _subject.Gate.Set();
                    _ctr.Dispose();
                    break;

                case EnumeratorState.Accepting:
                    Error = errorOpt;
                    Disposed = _subject.RemoveLocked(this);
                    State = EnumeratorState.Final;
                    Current = default;
                    _subject.Gate.Set();
                    _ctr.Dispose();
                    TsAccepting.SetExceptionOrResult(errorOpt, false);
                    break;

                case EnumeratorState.Emitting:
                    Error = errorOpt;
                    Disposed = _subject.RemoveLocked(this);
                    State = EnumeratorState.Final;
                    _subject.Gate.Set();
                    _ctr.Dispose();
                    TsEmitting.SetResult(false);
                    break;

                case EnumeratorState.Final:
                    _subject.Gate.Set();
                    break;

                default:
                    throw new Exception(State + "???");
            }
        }
    }
}