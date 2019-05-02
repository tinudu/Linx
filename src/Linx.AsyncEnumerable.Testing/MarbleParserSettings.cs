namespace Linx.AsyncEnumerable.Testing
{
    using System;

    /// <summary>
    /// Settings for the <see cref="MarbleParser"/>.
    /// </summary>
    public sealed class MarbleParserSettings
    {
        /// <summary>
        /// One second.
        /// </summary>
        public static TimeSpan DefaultFrameSize { get; }= TimeSpan.FromTicks(TimeSpan.TicksPerSecond);

        private TimeSpan _frameSize = DefaultFrameSize;

        /// <summary>
        /// Gets the duration of a time frame (default: one second).
        /// </summary>
        public TimeSpan FrameSize
        {
            get => _frameSize;
            set => _frameSize = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// The <see cref="Error"/> used as the replacement of the '#' completion (default: <see cref="MarbleException"/>).
        /// </summary>
        public Exception Error { get; set; }
    }
}
