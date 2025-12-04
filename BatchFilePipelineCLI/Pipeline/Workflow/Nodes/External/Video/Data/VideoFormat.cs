using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Data
{
    /// <summary>
    /// Store the basic information for a video file that has been processed
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class VideoFormat
    {
        /*----------Properties----------*/
        //PUBLIC

        [JsonProperty("filename")] public string? FileName { get; set; }
        [JsonProperty("nb_streams")] public string? StreamCount { get; set; }
        [JsonProperty("format_name")] public string? FormatName { get; set; }
        [JsonProperty("format_long_name")] public string? FormatLongName { get; set; }
        [JsonProperty("start_time")] public string? StartTime { get; set; }
        [JsonProperty("duration")] public string? Duration { get; set; }
        [JsonProperty("size")] public string? Size { get; set; }
        [JsonProperty("bit_rate")] public string? BitRate { get; set; }
        [JsonProperty("probe_score")] public int ProbeScore { get; set; }
        [JsonProperty("tags")] public Dictionary<string, string>? Tags { get; set; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Use the name of the file as the representation of this object
        /// </summary>
        public override string ToString() => FileName ?? string.Empty;
    }
}
