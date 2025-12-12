namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data
{
    /// <summary>
    /// Stores a collection of the different meta-data values that are contained for a video file
    /// </summary>
    public sealed class VideoDetails
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Store the basic details of the video for use
        /// </summary>
        public VideoFormat Format { get; set; } = new();

        /// <summary>
        /// Store the collection of data streams that are included within the output
        /// </summary>
        public List<IDataStream> Streams { get; set; } = new();

        /// <summary>
        /// The collection of chapters that are defined throughout the file
        /// </summary>
        public List<VideoChapter> Chapters { get; set; } = new();

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Use the underlying format details as the string representation
        /// </summary>
        public override string ToString() => Format.ToString();
    }
}
