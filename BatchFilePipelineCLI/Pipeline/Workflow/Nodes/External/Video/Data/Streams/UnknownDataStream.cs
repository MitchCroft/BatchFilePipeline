using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Data.Streams
{
    /// <summary>
    /// Define a wrapper around the description of a data stream that couldn't be classified
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class UnknownDataStream : IDataStream
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The index of the stream within the video file
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// The type of data that is contained within this stream
        /// </summary>
        public StreamType StreamType => StreamType.Unknown;

        /// <summary>
        /// The type of codec that was in use that couldn't be parsed into a workable stream object
        /// </summary>
        [JsonProperty("codec_type")]
        public string? CodecType { get; set; }

        /// <summary>
        /// The name of the codec that was used to encode the data
        /// </summary>
        [JsonProperty("codec_name")]
        public string? CodecName { get; set; }

        /// <summary>
        /// The full name of the codec that was used to encode the data
        /// </summary>
        [JsonProperty("codec_long_name")]
        public string? CodeLongName { get; set; }

        /// <summary>
        /// The tag that is applied from the codec
        /// </summary>
        [JsonProperty("codec_tag")]
        public string? CodecTag { get; set; }

        /// <summary>
        /// The long version of the codec tag as a display string
        /// </summary>
        [JsonProperty("codec_tag_string")]
        public string? CodecTagString { get; set; }

        /// <summary>
        /// The collection of disposition flags that are assigned to the data stream for processing
        /// </summary>
        [JsonProperty("disposition")]
        public DataStreamDisposition? Disposition { get; set; }

        /// <summary>
        /// Collection of generic tags that are attached to the stream for display
        /// </summary>
        [JsonProperty("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }
}
