using System;
using System.Collections.Generic;
using Linx.Timing;

namespace Linx.AsyncEnumerable
{
    partial class LinxAsyncEnumerable
    {
        /// <summary>
        /// Retry the sequence until it terminates successfully.
        /// </summary>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return Create<T>(async (yield, token) =>
            {
                while (true)
                    try
                    {
                        // ReSharper disable once PossibleMultipleEnumeration
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

            return Create<T>(async (yield, token) =>
            {
                Exception error;
                try
                {
                    // ReSharper disable once PossibleMultipleEnumeration
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
                        // ReSharper disable once PossibleMultipleEnumeration
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
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, TimeSpan waitTime, ITime time)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (time is null) time = Time.RealTime;

            return Create<T>(async (yield, token) =>
            {
                try
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    await source.CopyTo(yield, token).ConfigureAwait(false);
                    return;
                }
                catch { token.ThrowIfCancellationRequested(); }

                using var timer = time.GetTimer(token);
                while (true)
                    try
                    {
                        await timer.Delay(waitTime).ConfigureAwait(false);
                        // ReSharper disable once PossibleMultipleEnumeration
                        await source.CopyTo(yield, token).ConfigureAwait(false);
                        return;
                    }
                    catch { token.ThrowIfCancellationRequested(); }
            });
        }

        /// <summary>
        /// Retry the sequence, waiting between retries for each element in <paramref name="waitTimes"/>.
        /// </summary>
        public static IAsyncEnumerable<T> Retry<T>(this IAsyncEnumerable<T> source, IEnumerable<TimeSpan> waitTimes, ITime time)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (waitTimes == null) throw new ArgumentNullException(nameof(waitTimes));
            if (time is null) time = Time.RealTime;

            return Create<T>(async (yield, token) =>
            {
                Exception error;
                try
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    await source.CopyTo(yield, token).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    token.ThrowIfCancellationRequested();
                    error = ex;
                }

                using (var timer = time.GetTimer(token))
                    foreach (var waitTime in waitTimes)
                        try
                        {
                            await timer.Delay(waitTime).ConfigureAwait(false);
                            // ReSharper disable once PossibleMultipleEnumeration
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