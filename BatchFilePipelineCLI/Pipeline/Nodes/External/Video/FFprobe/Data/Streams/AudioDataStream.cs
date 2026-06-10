using BatchFilePipelineCLI.Pipeline.Nodes.External.Video.FFprobe.Data;
using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.Video.FFprobe.Data.Streams
{
    /// <summary>
    /// Contain a collection of data about an audio stream that is being processed
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class AudioDataStream : IDataStream
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
        public StreamType StreamType => StreamType.Audio;

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
        [JsonProperty("sample_fmt")] public string? SampleFormat { get; set; }
        [JsonProperty("sample_rate")] public string? SampleRate { get; set; }
        [JsonProperty("channels")] public int Channels { get; set; }
        [JsonProperty("channel_layout")] public string? ChannelLayout { get; set; }
        [JsonProperty("bits_per_sample")] public int BitsPerSample { get; set; }
        [JsonProperty("initial_padding")] public int InitialPadding { get; set; }
        [JsonProperty("dmix_mode")] public string? DmixMode { get; set; }
        [JsonProperty("ltrt_cmixlev")] public string? LtrtCmixlev { get; set; }
        [JsonProperty("ltrt_surmixlev")] public string? LtrtSurmixlev { get; set; }
        [JsonProperty("loro_cmixlev")] public string? LoroCmixlev { get; set; }
        [JsonProperty("loro_surmixlev")] public string? LoroSurmixlev { get; set; }
        [JsonProperty("r_frame_rate")] public string? RFrameRate { get; set; }
        [JsonProperty("avg_frame_rate")] public string? AverageFrameRate { get; set; }
        [JsonProperty("time_base")] public string? TimeBase { get; set; }
        [JsonProperty("start_pts")] public int StartPts { get; set; }
        [JsonProperty("start_time")] public string? StartTime { get; set; }
        [JsonProperty("bit_rate")] public string? BitRate { get; set; }
        [JsonProperty("extradata")] public string? ExtraData { get; set; }
    }
}
