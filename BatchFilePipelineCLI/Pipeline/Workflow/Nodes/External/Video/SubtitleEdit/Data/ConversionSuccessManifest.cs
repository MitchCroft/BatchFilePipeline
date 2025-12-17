using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.SubtitleEdit.Data
{
    /// <summary>
    /// Store a collection of information that describes the subtitle conversion success state
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ConversionSuccessManifest
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The collection of different streams that were included in the process
        /// </summary>
        [JsonProperty] public List<SubtitleStreamConversionSummary> Streams { get; set; } = new();

        /// <summary>
        /// The average success rate evaluation across all streams
        /// </summary>
        [JsonProperty] public float AverageSuccess { get; set; } = 0f;

        /// <summary>
        /// The minimum success rate evaluation across all streams
        /// </summary>
        [JsonProperty] public float MinSuccess { get; set; } = 0f;

        /// <summary>
        /// The maximum success rate evaluation across all streams
        /// </summary>
        [JsonProperty] public float MaxSuccess { get; set; } = 0f;
    }

    /// <summary>
    /// Store a collection of values that describe the conversion success of a Subtitle Stream
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SubtitleStreamConversionSummary
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The index of the stream that was processed
        /// </summary>
        [JsonProperty] public int Index { get; set; }

        /// <summary>
        /// Normalised percentage value that describes an estimate of how many words were successfully converted
        /// </summary>
        [JsonProperty] public float Successful { get; set; } = 0f;

        /// <summary>
        /// The collection of words that were unknown during the evaluation step
        /// </summary>
        [JsonProperty] public List<string> FailedWords { get; set; } = new();
    }
}
