using Linx.Tasks;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Linx
{
    partial class Linx
    {
        /// <summary>
        /// Complete with an error or the specified result.
        /// </summary>
        public static void SetExceptionOrResult<T>(this ManualResetValueTaskSource<T> ts, Exception? exceptionOrNot, T result)
        {
            if (ts is null) throw new ArgumentNullException(nameof(ts));

            if (exceptionOrNot is null)
                ts.SetResult(result);
            else
                ts.SetException(exceptionOrNot);
        }

        /// <summary>
        /// Complete with or without an error.
        /// </summary>
        public static void SetExceptionOrResult(this ManualResetValueTaskSource ts, Exception? exceptionOrNot)
        {
            if (ts is null) throw new ArgumentNullException(nameof(ts));

            if (exceptionOrNot is null)
                ts.SetResult();
            else
                ts.SetException(exceptionOrNot);
        }

        /// <summary>
        /// Complete with an error or the specified result.
        /// </summary>
        public static void SetExceptionOrResult<T>(this ref AsyncTaskMethodBuilder<T> ts, Exception? exceptionOrNot, T result)
        {
            if (exceptionOrNot is null)
                ts.SetResult(result);
            else
                ts.SetException(exceptionOrNot);
        }

        /// <summary>
        /// Complete with or without an error.
        /// </summary>
        public static void SetExceptionOrResult(this ref AsyncTaskMethodBuilder ts, Exception? exceptionOrNot)
        {
            if (exceptionOrNot is null)
                ts.SetResult();
            else
                ts.SetException(exceptionOrNot);
        }

        /// <summary>
        /// Complete with an error or the specified result.
        /// </summary>
        public static void SetExceptionOrResult<T>(this TaskCompletionSource<T> ts, Exception? exceptionOrNot, T result)
        {
            if (ts is null) throw new ArgumentNullException(nameof(ts));

            if (exceptionOrNot is null)
                ts.SetResult(result);
            else
                ts.SetException(exceptionOrNot);
        }

        /// <summary>
        /// Complete with or without an error.
        /// </summary>
        public static void SetExceptionOrResult(this TaskCompletionSource ts, Exception? exceptionOrNot)
        {
            if (ts is null) throw new ArgumentNullException(nameof(ts));

            if (exceptionOrNot is null)
                ts.SetResult();
            else
                ts.SetException(exceptionOrNot);
        }
    }
}
