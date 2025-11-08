using System.Xml.Serialization;

namespace BatchFilePipelineCLI.Pipeline.Description
{
    /// <summary>
    /// The collection of node definitions that can be used when processing a workflow
    /// </summary>
    public sealed class WorkflowDescription
    {
        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// The graph elements that will be processed *once* before the main process is run for all file entries collected
        /// </summary>
        [XmlElement("PreProcessGraph")]
        public NodeDescription[] PreProcessGraph { get; set; } = Array.Empty<NodeDescription>();

        /// <summary>
        /// The collection of graph elements that will be run over all identified files
        /// </summary>
        [XmlElement("ProcessGraph")]
        public NodeDescription[] ProcessGraph { get; set; } = Array.Empty<NodeDescription>();

        /// <summary>
        /// The graph elements that will be processed *once* after the main process is run for all file entries collected
        /// </summary>
        [XmlElement("PostProcessGraph")]
        public NodeDescription[] PostProcessGraph { get; set; } = Array.Empty<NodeDescription>();
    }
}
