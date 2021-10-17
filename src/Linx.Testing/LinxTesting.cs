using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Linx.Testing
{
    /// <summary>
    /// Static Linx.Testing methods.
    /// </summary>
    public static partial class LinxTesting
    {
        /// <summary>
        /// Gets the default time frame.
        /// </summary>
        public static TimeSpan DefaultTimeFrame { get; } = TimeSpan.FromTicks(TimeSpan.TicksPerSecond);

        /// <summary>
        /// Expect the characters as specified in <paramref name="pattern"/>, using the <see cref="DefaultTimeFrame"/>.
        /// </summary>
        public static Task Expect(
            this IAsyncEnumerable<char> source,
            string pattern,
            VirtualTime virtualTime,
            Func<Exception, bool> exceptionEquals = null)
            => source.Expect(pattern, DefaultTimeFrame, virtualTime, (x, i, y) => x == y, exceptionEquals);

        /// <summary>
        /// Expect the characters as specified in <paramref name="pattern"/>.
        /// </summary>
        public static Task Expect(
            this IAsyncEnumerable<char> source,
            string pattern,
            TimeSpan timeFrame,
            VirtualTime virtualTime,
            Func<Exception, bool> exceptionEquals = null)
            => source.Expect(pattern, timeFrame, virtualTime, (x, i, y) => x == y, exceptionEquals);

        /// <summary>
        /// Expect using the <see cref="DefaultTimeFrame"/>.
        /// </summary>
        public static Task Expect<T>(
            this IAsyncEnumerable<T> source,
            string pattern,
            VirtualTime virtualTime,
            Func<char, int, T, bool> equals,
            Func<Exception, bool> exceptionEquals = null)
            => source.Expect(pattern, DefaultTimeFrame, virtualTime, equals, exceptionEquals);

        /// <summary>
        /// Enumerate <paramref name="source"/> and make sure it represents the expected sequence.
        /// </summary>
        /// <param name="source">The sequence to test.</param>
        /// <param name="pattern">The expectation pattern.</param>
        /// <param name="timeFrame">The length of a time frame.</param>
        /// <param name="virtualTime">The <see cref="VirtualTime"/> on which to run the test.</param>
        /// <param name="equals">Element equality function.</param>
        /// <param name="exceptionEquals">Exception equality function.</param>
        public static Task Expect<T>(
            this IAsyncEnumerable<T> source,
            string pattern,
            TimeSpan timeFrame,
            VirtualTime virtualTime,
            Func<char, int, T, bool> equals,
            Func<Exception, bool> exceptionEquals = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (pattern is null) throw new ArgumentNullException(nameof(pattern));
            if (timeFrame <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeFrame));
            if (virtualTime is null) throw new ArgumentNullException(nameof(virtualTime));
            if (equals is null) throw new ArgumentNullException(nameof(equals));
            if (exceptionEquals is null) exceptionEquals = ex => ex == TestException.Singleton;

            throw new NotImplementedException();
        }
    }
}
