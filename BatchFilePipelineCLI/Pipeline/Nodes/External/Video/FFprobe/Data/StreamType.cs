namespace BatchFilePipelineCLI.Pipeline.Nodes.External.Video.FFprobe.Data
{
    /// <summary>
    /// Flag the different types of data stream are available in a video file
    /// </summary>
    public enum StreamType
    {
        /// <summary>
        /// Unable to determine what the stream is
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Stream is a video stream with visual data
        /// </summary>
        Video = 1,

        /// <summary>
        /// Stream is an audio stream with audio data
        /// </summary>
        Audio = 2,

        /// <summary>
        /// Stream contains subtitle information 
        /// </summary>
        Subtitle = 3,

        /// <summary>
        /// Indicates various types of data streams, sometimes used for things like chapter markers or other metadata.
        /// </summary>
        Data = 4,

        /// <summary>
        /// Used for embedded files, such as cover art in an audio file
        /// </summary>
        Attachment = 5,
    }
}
