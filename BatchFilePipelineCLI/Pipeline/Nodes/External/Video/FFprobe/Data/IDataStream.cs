namespace BatchFilePipelineCLI.Pipeline.Nodes.External.Video.FFprobe.Data
{
    /// <summary>
    /// Define a collection of values that exist within a video file stream
    /// </summary>
    public interface IDataStream
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The index of the stream within the video file
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The type of data that is contained within this stream
        /// </summary>
        public StreamType StreamType { get; }

        /// <summary>
        /// The name of the codec that was used to encode the data
        /// </summary>
        public string? CodecName { get; }

        /// <summary>
        /// The full name of the codec that was used to encode the data
        /// </summary>
        public string? CodeLongName { get; }

        /// <summary>
        /// The tag that is applied from the codec
        /// </summary>
        public string? CodecTag { get; }

        /// <summary>
        /// The long version of the codec tag as a display string
        /// </summary>
        public string? CodecTagString { get; }

        /// <summary>
        /// The collection of disposition flags that are assigned to the data stream for processing
        /// </summary>
        public DataStreamDisposition? Disposition { get; }

        /// <summary>
        /// Collection of generic tags that are attached to the stream for display
        /// </summary>
        public Dictionary<string, string>? Tags { get; }
    }
}
