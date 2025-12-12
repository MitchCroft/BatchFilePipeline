using BatchFilePipelineCLI.Logging;
using BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data;
using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.FFprobe.Data.Streams
{
    /// <summary>
    /// Contain a collection of data about a subtitle stream that is being processed
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class SubtitleDataStream : IDataStream
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
        public StreamType StreamType => StreamType.Subtitle;

        /// <summary>
        /// Get the type of subtitle based on the codec that was used
        /// </summary>
        public SubtitleType SubtitleType
        {
            get
            {
                // If there is no codec specified, nothing we can do
                if (string.IsNullOrWhiteSpace(CodecName) == true)
                {
                    return SubtitleType.Unknown;
                }

                // Test for the known types
                switch (CodecName)
                {
                    // Text based
                    case "subrip":
                    case "ass":
                    case "ssa":
                    case "webvtt":
                    case "text":    return SubtitleType.Text;

                    // Image based
                    case "dvd_subtitle":
                    case "hdmv_pgs_subtitle":
                    case "xsub":    return SubtitleType.Image;

                    // Unknown
                    default:
                        Logger.Warning($"[{nameof(SubtitleDataStream)}] Unknown codec used '{CodecName}', unable to determine subtitle type");
                        return SubtitleType.Unknown;
                }
            }
        }

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

        [JsonProperty("r_frame_rate")] public string? RFrameRate { get; set; }
        [JsonProperty("avg_frame_rate")] public string? AverageFrameRate { get; set; }
        [JsonProperty("time_base")] public string? TimeBase { get; set; }
        [JsonProperty("start_pts")] public string? StartPts { get; set; }
        [JsonProperty("start_time")] public string? StartTime { get; set; }
        [JsonProperty("duration_ts")] public int Duration { get; set; }
        [JsonProperty("duration")] public string? DurationString { get; set; }
        [JsonProperty("extradata")] public string? ExtraData { get; set; }
    }

    /// <summary>
    /// Classify the different types of subtitles that are found in a video file
    /// </summary>
    public enum SubtitleType
    {
        /// <summary>
        /// Unable to determine the type of subtitle in use
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Subtitles are text based
        /// </summary>
        Text = 1,

        /// <summary>
        /// Subtitles are image based
        /// </summary>
        Image = 2,
    }
}
