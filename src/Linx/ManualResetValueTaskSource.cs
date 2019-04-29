namespace Linx
{
    using System;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Sources;

    internal static class ManualResetValueTaskSource
    {
        public static ValueTask<TResult> GenericTask<TResult>(this ManualResetValueTaskSource<TResult> source) => new ValueTask<TResult>(source, source.Version);
        public static ValueTask Task<TResult>(this ManualResetValueTaskSource<TResult> source) => new ValueTask(source, source.Version);

        public static void SetExceptionOrResult<TResult>(this ManualResetValueTaskSource<TResult> source, Exception exception, TResult result)
        {
            if (exception == null) source.SetResult(result);
            else source.SetException(exception);
        }
    }
}
