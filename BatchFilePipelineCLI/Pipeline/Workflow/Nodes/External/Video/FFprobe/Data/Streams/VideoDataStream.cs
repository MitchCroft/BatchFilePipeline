using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data;
using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams
{
    /// <summary>
    /// Contain a collection of data about a video stream that is being processed
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class VideoDataStream : IDataStream
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
        public StreamType StreamType => StreamType.Video;

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

        [JsonProperty("profile")] public string? Profile { get; set; }
        [JsonProperty("width")] public int Width { get; set; }
        [JsonProperty("height")] public int Height { get; set; }
        public int Area => Width * Height;
        [JsonProperty("coded_width")] public int CodedWidth { get; set; }
        [JsonProperty("coded_height")] public int CodedHeight { get; set; }
        public int CodedArea => CodedWidth * CodedHeight;
        [JsonProperty("has_b_frames")] public int HasBFrames { get; set; }
        [JsonProperty("sample_aspect_ratio")] public string? SampleAspectRatio { get; set; }
        [JsonProperty("display_aspect_ratio")] public string? DisplayAspectRatio { get; set; }
        [JsonProperty("pix_fmt")] public string? PixelFormat { get; set; }
        [JsonProperty("level")] public int Level { get; set; }
        [JsonProperty("color_range")] public string? ColorRange { get; set; }
        [JsonProperty("color_space")] public string? ColorSpace { get; set; }
        [JsonProperty("color_transfer")] public string? ColorTransfer { get; set; }
        [JsonProperty("color_primaries")] public string? ColorPrimaries { get; set; }
        [JsonProperty("chroma_location")] public string? ChromaLocation { get; set; }
        [JsonProperty("field_order")] public string? FieldOrder { get; set; }
        [JsonProperty("refs")] public int Refs { get; set; }
        [JsonProperty("is_avc")] public string? IsAvc { get; set; }
        [JsonProperty("nal_length_size")] public string? NalLengthSize { get; set; }
        [JsonProperty("r_frame_rate")] public string? RFrameRate { get; set; }
        [JsonProperty("avg_frame_rate")] public string? AverageFrameRate { get; set; }
        [JsonProperty("time_base")] public string? TimeBase { get; set; }
        [JsonProperty("start_pts")] public string? StartPts { get; set; }
        [JsonProperty("start_time")] public string? StartTime { get; set; }
        [JsonProperty("bits_per_raw_sample")] public string? BitsPerRawSample { get; set; }
        [JsonProperty("extradata")] public string? ExtraData { get; set; }
        [JsonProperty("extradata_size")] public int ExtraDataSize { get; set; }
    }
}
