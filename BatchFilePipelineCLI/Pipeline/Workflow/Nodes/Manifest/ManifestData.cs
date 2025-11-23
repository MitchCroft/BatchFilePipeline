namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.Manifest
{
    /// <summary>
    /// Store a collection of generic data that can be used as a manifest for processing identified files
    /// at runtime via the workflow
    /// </summary>
    internal sealed class ManifestData
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Store a generic collection of keyed data that can be used for each identified element for
        /// tracking persistent information between runs of the workflow
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> Data { get; set; } = new();
    }
}
