using Newtonsoft.Json;

namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.Handbrake.Data
{
    /// <summary>
    /// Represent the root object for the Handbrake preset manifest file
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public sealed class HandbrakePresetManifestRoot
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The collection of available presets in the file
        /// </summary>
        [JsonProperty("PresetList")]
        public HandbrakePresetOption[] PresetList { get; set; } = Array.Empty<HandbrakePresetOption>();
    }
}
