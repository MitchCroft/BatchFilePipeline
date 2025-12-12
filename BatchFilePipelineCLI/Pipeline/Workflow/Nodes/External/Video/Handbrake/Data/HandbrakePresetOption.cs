using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake.Data
{
    /// <summary>
    /// Define the collection of properties that are needed for extracting the required values for directing applied profiles
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class HandbrakePresetOption
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The name of the preset that can be selected for use
        /// </summary>
        [JsonProperty("PresetName")]
        public string? PresetName { get; set; }

        /// <summary>
        /// The file format that the output file will be generated as, expecting "av_mp4" or "av_mkv"
        /// </summary>
        [JsonProperty("FileFormat")]
        public string? FileFormat { get; set; }

        /// <summary>
        /// The width of the picture output that will be used
        /// </summary>
        [JsonProperty("PictureWidth")]
        public int PictureWidth { get; set; }

        /// <summary>
        /// The height of the picture output that will be used
        /// </summary>
        [JsonProperty("PictureHeight")]
        public int PictureHeight { get; set; }

        /// <summary>
        /// Encoder settings that are to be used for the video stream
        /// </summary>
        [JsonProperty("VideoEncoder")]
        public string? VideoEncoder { get; set; }

        /// <summary>
        /// Check that the preset option is valid for use
        /// </summary>
        public bool IsValid => string.IsNullOrWhiteSpace(PresetName) == false &&
            PictureWidth > 0 &&
            PictureHeight > 0;
    }
}
