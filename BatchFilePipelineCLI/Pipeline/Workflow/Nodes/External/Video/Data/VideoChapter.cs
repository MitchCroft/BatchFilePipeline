using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Data
{
    /// <summary>
    /// Store a collection of values describing the chapters of a video file
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class VideoChapter
    {
        /*----------Properties----------*/
        //PUBLIC

        [JsonProperty("id")] public int Id { get; set; }
        public string Title => Tags?.TryGetValue("title", out var title) ?? false ? title : "[UNTITLED]";
        [JsonProperty("time_base")] public string? TimeBase { get; set; }
        [JsonProperty("start")] public long Start { get; set; }
        [JsonProperty("start_time")] public string? StartTime { get; set; }
        [JsonProperty("end")] public long End { get; set; }
        [JsonProperty("end_time")] public string? EndTime { get; set; }
        [JsonProperty("tags")] public Dictionary<string, string>? Tags { get; set; }

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Use the title of the chapter as the string representation
        /// </summary>
        public override string ToString() => Title;
    }
}
