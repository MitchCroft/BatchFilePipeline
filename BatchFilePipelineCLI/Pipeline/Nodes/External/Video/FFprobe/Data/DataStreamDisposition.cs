using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Nodes.External.Video.FFprobe.Data
{
    /// <summary>
    /// Store a collection of disposition flags that correlate to a stream of data
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class DataStreamDisposition
    {
        /*----------Properties----------*/
        //PUBLIC

        [JsonProperty("default")] public int Default { get; set; }
        [JsonProperty("dub")] public int Dub { get; set; }
        [JsonProperty("original")] public int Original { get; set; }
        [JsonProperty("comment")] public int Comment { get; set; }
        [JsonProperty("lyrics")] public int Lyrics { get; set; }
        [JsonProperty("karaoke")] public int Karaoke { get; set; }
        [JsonProperty("forced")] public int Forced { get; set; }
        [JsonProperty("hearing_impaired")] public int HearingImpaired { get; set; }
        [JsonProperty("visual_impaired")] public int VisualImpaired { get; set; }
        [JsonProperty("clean_effects")] public int CleanEffects { get; set; }
        [JsonProperty("attached_pic")] public int AttachedPic { get; set; }
        [JsonProperty("timed_thumbnails")] public int TimedThumbnails { get; set; }
        [JsonProperty("non_diegetic")] public int NonDiegetic { get; set; }
        [JsonProperty("captions")] public int Captions { get; set; }
        [JsonProperty("descriptions")] public int Descriptions { get; set; }
        [JsonProperty("metadata")] public int MetaData { get; set; }
        [JsonProperty("dependent")] public int Dependent { get; set; }
        [JsonProperty("still_image")] public int StillImage { get; set; }
        [JsonProperty("multilayer")] public int Multilayer { get; set; }
    }
}
