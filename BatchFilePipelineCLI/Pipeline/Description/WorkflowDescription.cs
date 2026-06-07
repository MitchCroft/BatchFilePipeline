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
        [XmlAttribute("EntryGraph")]
        public string? EntryGraph { get; set; } = string.Empty;

        /// <summary>
        /// Flags if this workflow description is a module of graphs that can be run
        /// </summary>
        /// <remarks>
        /// A module is a collection of functionality, but it can't be run directly as a workflow.
        /// It must be referenced by another executing workflow description that specifies an entry graph.
        /// </remarks>
        public bool IsModule => string.IsNullOrWhiteSpace(EntryGraph);

        /// <summary>
        /// Deines the collection of graph elements within this workflow that can be used to process
        /// functionality while running a workflow
        /// </summary>
        [XmlElement("Graph")]
        public GraphDescription[] Graphs { get; set; } = Array.Empty<GraphDescription>();
    }
}
