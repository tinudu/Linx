namespace Linx.AsyncEnumerable
{
    using System;
    using System.Collections.Generic;
    using Timing;

    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Retry the sequence until it terminates successfully.
        /// </summary>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                while (true)
                    try
                    {
                        await source.CopyTo(yield, token).ConfigureAwait(false);
                        return;
                    }
                    catch { token.ThrowIfCancellationRequested(); }
            });
        }

        /// <summary>
        /// Retry the sequence up to <paramref name="retryCount"/> times.
        /// </summary>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, int retryCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                Exception error;
                try
                {
                    await source.CopyTo(yield, token).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    token.ThrowIfCancellationRequested();
                    error = ex;
                }

                for (var r = retryCount; r > 0; r--)
                {
                    try
                    {
                        await source.CopyTo(yield, token).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        token.ThrowIfCancellationRequested();
                        error = ex;
                    }
                }

                throw error;
            });
        }

        /// <summary>
        /// Retry the sequence until it terminates successfully, waiting <paramref name="waitTime"/> between retries.
        /// </summary>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, TimeSpan waitTime)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Produce<T>(async (yield, token) =>
            {
                var time = Time.Current;
                try
                {
                    await source.CopyTo(yield, token).ConfigureAwait(false);
                    return;
                }
                catch { token.ThrowIfCancellationRequested(); }

                while (true)
                    try
                    {
                        await time.Delay(waitTime, token).ConfigureAwait(false);
                        await source.CopyTo(yield, token).ConfigureAwait(false);
                        return;
                    }
                    catch { token.ThrowIfCancellationRequested(); }
            });
        }

        /// <summary>
        /// Retry the sequence, waiting between retries for each element in <paramref name="waitTimes"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, IEnumerable<TimeSpan> waitTimes)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (waitTimes == null) throw new ArgumentNullException(nameof(waitTimes));

            return Produce<T>(async (yield, token) =>
            {
                var time = Time.Current;
                Exception error;
                try
                {
                    await source.CopyTo(yield, token).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    token.ThrowIfCancellationRequested();
                    error = ex;
                }

                foreach (var waitTime in waitTimes)
                    try
                    {
                        await time.Delay(waitTime, token).ConfigureAwait(false);
                        await source.CopyTo(yield, token).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception ex)
                    {
                        token.ThrowIfCancellationRequested();
                        error = ex;
                    }

                throw error;
            });
        }
    }
}