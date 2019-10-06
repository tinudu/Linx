namespace Linx.AsyncEnumerable
{
    using System.Collections.Generic;

    /// <summary>
    /// Static Linx.AsyncEnumerable methods.
    /// </summary>
    public static partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Invoke the specified <paramref name="stack"/> in order.
        /// </summary>
        public static void Connect(this Stack<ConnectDelegate> stack)
        {
            while (stack.Count > 0)
                stack.Pop()();
        }
    }
}
