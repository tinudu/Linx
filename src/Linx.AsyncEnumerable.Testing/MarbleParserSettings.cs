namespace Linx.AsyncEnumerable.Testing
{
    using System;

    /// <summary>
    /// Settings for the <see cref="MarbleParser"/>.
    /// </summary>
    public sealed class MarbleParserSettings
    {
        private TimeSpan _frameSize = TimeSpan.FromTicks(TimeSpan.TicksPerSecond);

        /// <summary>
        /// Gets the duration of a time frame (default: one second).
        /// </summary>
        public TimeSpan FrameSize
        {
            get => _frameSize;
            set => _frameSize = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException();
        }

        /// <summary>
        /// The <see cref="Exception"/> used as the replacement of the '#' completion (default: <see cref="MarbleException"/>).
        /// </summary>
        public Exception Exception { get; set; }
    }
}
