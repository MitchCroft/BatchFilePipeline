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
        /// Defines the ID of the graph within this workflow description that should be used as the entry point when running the workflow directly
        /// </summary>
        [XmlAttribute(nameof(EntryGraphId))]
        public string? EntryGraphId { get; set; } = string.Empty;

        /// <summary>
        /// Deines the collection of graph elements within this workflow that can be used to process
        /// functionality while running a workflow
        /// </summary>
        [XmlElement("Graph")]
        public GraphDescription[] Graphs { get; set; } = Array.Empty<GraphDescription>();
    }
}
